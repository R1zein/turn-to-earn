using UnityEngine;
using Zenject;

// Arcade plane in the spirit of Fortnite's: it flies where the nose points at the
// speed the throttle sets, never stalls, and with the throttle at zero it brakes
// and hangs in the air like a VTOL. Physics stays honest about inertia: the
// rotors/engine push toward a WANTED velocity with limited acceleration - fast
// along the nose, firmer sideways ("grip") - so a hard turn at speed drifts a
// little. Gravity is on and the engine always carries the weight.
//
// Third person (chase camera): the mouse moves an invisible on-screen CURSOR (the
// HUD ring is drawn on it); an autopilot points the nose where the cursor says,
// the way a pilot would: pitch and a little rudder toward it, and a bank into the
// turn. The chase camera sits behind the drone's heading, so the screen centre
// (the static diamond) means "straight and level": a cursor left to the side
// keeps the drone turning, a cursor above the centre holds a climb at that angle.
// Q/E take the roll over - hold for barrel rolls; released, the autopilot rolls
// back level. A/D steer while held.
//
// First person (Aim, RMB held - the sniping stance): the camera moves to the
// nose (DroneCamera) and the mouse turns a target attitude directly, like FPS
// mouse-look (GameConfig.DroneAimSensitivity deg per pixel); the nose snaps onto
// it at full speed. The drone brakes to a hover and W/S/A/D/Space/Ctrl move it
// directly - forward/back, sideways, up/down - like a quadcopter. The throttle is
// kept and takes over again on release; the cursor waits in the centre.
//
// Rotation always goes through torque: a body-rate controller turns commanded
// rates into angular acceleration.
// Body axes in Unity: +X rate = nose down, +Y = yaw right, +Z = roll left.
[RequireComponent(typeof(Rigidbody))]
public class DroneControl : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Speed at full throttle, m/s")]
    [SerializeField] private float cruiseSpeed = 40f;
    [Tooltip("Speed while Boost is held, m/s")]
    [SerializeField] private float boostSpeed = 60f;
    [Tooltip("Throttle change per second while W/S is held")]
    [SerializeField] private float throttleRate = 0.6f;
    [Tooltip("Up/down speed with Space/Ctrl, m/s")]
    [SerializeField] private float climbSpeed = 10f;
    [Tooltip("Aiming: W/S/A/D move the drone directly at this speed, m/s")]
    [SerializeField] private float aimMoveSpeed = 12f;

    [Header("Inertia")]
    [Tooltip("1/s: how fast the speed along the nose reaches the wanted one")]
    [SerializeField] private float speedResponse = 1.2f;
    [Tooltip("Acceleration/braking limit along the nose, m/s^2")]
    [SerializeField] private float maxAcceleration = 20f;
    [Tooltip("1/s: how fast sideways/vertical slip is killed - the wings' grip")]
    [SerializeField] private float gripResponse = 6f;
    [Tooltip("Sideways acceleration limit, m/s^2 (~9 g - a full-rate loop at cruise speed needs 8.5): beyond it a turn drifts")]
    [SerializeField] private float maxGrip = 90f;

    [Header("Autopilot (third person)")]
    [Tooltip("1/s: nose error (deg) -> pitch rate")]
    [SerializeField] private float pitchResponse = 4f;
    [SerializeField] private float maxPitchRate = 120f;
    [Tooltip("1/s: nose error (deg) -> rudder rate")]
    [SerializeField] private float yawResponse = 3f;
    [SerializeField] private float maxYawRate = 90f;
    [Tooltip("Bank per degree of sideways nose error, at speed")]
    [SerializeField] private float bankPerDegree = 2.5f;
    [SerializeField] private float maxBank = 70f;
    [Tooltip("Below this speed the drone turns flat (hovering), m/s")]
    [SerializeField] private float bankSpeed = 15f;
    [Tooltip("1/s: bank error (deg) -> roll rate")]
    [SerializeField] private float rollResponse = 6f;

    [Header("Aiming (first person)")]
    [Tooltip("How hard the nose snaps onto the aim, 1/s")]
    [SerializeField] private float aimHoldResponse = 15f;
    [Tooltip("Fastest the nose turns while aiming, deg/s")]
    [SerializeField] private float aimMaxRate = 360f;
    [Tooltip("Seconds of a resting mouse before the aim's wings level with the horizon")]
    [SerializeField] private float aimLevelDelay = 0.4f;
    [Tooltip("How fast they level, deg/s")]
    [SerializeField] private float aimLevelRate = 90f;

    [Header("Manual")]
    [Tooltip("Q/E roll rate, deg/s")]
    [SerializeField] private float rollRate = 270f;
    [Tooltip("A/D swing the aim this far to the side while held, deg")]
    [SerializeField] private float keyTurnAngle = 45f;
    [Tooltip("1/s: how fast the body reaches a commanded rate")]
    [SerializeField] private float rateResponse = 12f;

    [Tooltip("The cursor stays this many pixels inside the screen edge")]
    [SerializeField] private float cursorMargin = 24f;

    [Inject] private InputService input;
    [Inject] private GameConfig config;
    [Inject] private Camera sceneCamera;

    private Rigidbody rb;
    private DroneShooting shooting;
    private DroneBombBay bombBay;

    // Kept as an offset from the screen centre: the centre stays exact even if the
    // screen size changes after Awake (it does in the editor), or the window is resized.
    private Vector2 cursorOffset;
    private float throttle;
    private float roll;     // Q/E: -1 left .. 1 right
    private float lift;     // Space/Ctrl: -1 down .. 1 up
    private float keyMove;  // W/S while aiming: -1 back .. 1 forward
    private float keySide;  // A/D: -1 left .. 1 right
    private bool boost;
    private bool aiming;

    // Aiming: the attitude the mouse points, the mouse turn not yet applied to it
    // (deg) and how long the mouse has rested.
    private Quaternion aim;
    private Vector2 aimLook;
    private float aimRestTime;

    private Vector3 lastPosition;
    private float distanceTravelled;

    // The invisible cursor the pilot steers with, in screen pixels (origin bottom-left).
    public Vector2 AimScreenPoint => new Vector2(Screen.width, Screen.height) * 0.5f + cursorOffset;
    // Aiming: where the mouse has pointed the nose.
    public Vector3 AimDirection => aim * Vector3.forward;
    public float Throttle => throttle;
    public bool IsBoosting => boost;
    public bool IsAiming => aiming;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();
        bombBay = GetComponent<DroneBombBay>();
        // Default 7 rad/s (400 deg/s) would clip fast rolls.
        rb.maxAngularVelocity = 20f;

        aim = Quaternion.LookRotation(transform.forward, Vector3.up);
        lastPosition = rb.position;
    }

    private void Update()
    {
        ReadInput();
        MoveCursor();

        if (shooting != null)
            shooting.Shoot();
        if (bombBay != null)
            bombBay.HandleInput();
    }

    private void FixedUpdate()
    {
        if (aiming)
            RotateAiming();
        else
            Rotate();
        Fly();
        TrackDistance();
    }

    // Telemetry read from the outside. Nothing here knows who is reading it.
    public float GetSpeed() => rb.linearVelocity.magnitude;

    public float GetAltitude() => rb.position.y; // world zero is the reference height

    public float GetDistanceTravelled() => distanceTravelled;

    // Sum of the per-step moves, so it measures the path actually flown rather
    // than the straight line back to the start.
    private void TrackDistance()
    {
        Vector3 position = rb.position;
        distanceTravelled += Vector3.Distance(lastPosition, position);
        lastPosition = position;
    }

    private void ReadInput()
    {
        bool wasAiming = aiming;
        aiming = input.IsDroneAimHeld;
        // The aim starts where the nose points, so pressing Aim does not jerk the drone.
        if (aiming && !wasAiming)
        {
            aim = Quaternion.LookRotation(transform.forward, Vector3.up);
            aimLook = Vector2.zero;
        }

        // While aiming W/S move the drone instead of setting the throttle.
        keyMove = aiming ? input.DroneThrottle : 0f;
        if (!aiming)
            throttle = Mathf.Clamp01(throttle + input.DroneThrottle * throttleRate * Time.deltaTime);
        keySide = input.DroneYaw;
        roll = input.DroneRoll;
        lift = input.DroneLift;
        boost = input.IsBoostHeld;
    }

    // Third person: the cursor moves with the mouse 1:1 (times DroneCursorSpeed) and
    // stays where it was left, like a normal cursor - it is only invisible.
    // Aiming: the mouse turns the aim instead, and the cursor waits in the centre.
    private void MoveCursor()
    {
        Vector2 look = input.DroneLook;
        if (aiming)
        {
            aimLook += look * config.DroneAimSensitivity;
            cursorOffset = Vector2.zero;
            return;
        }

        Vector2 half = new Vector2(Screen.width, Screen.height) * 0.5f - Vector2.one * cursorMargin;
        cursorOffset += look * config.DroneCursorSpeed;
        cursorOffset.x = Mathf.Clamp(cursorOffset.x, -half.x, half.x);
        cursorOffset.y = Mathf.Clamp(cursorOffset.y, -half.y, half.y);
    }

    // Direction under the cursor, measured from the screen centre rather than from
    // the camera's slight look-down: the centre means level flight along the
    // camera's heading. Swung sideways while A/D are held.
    private Vector3 CursorDirection()
    {
        Transform view = sceneCamera.transform;
        Vector3 ray = sceneCamera.ScreenPointToRay(AimScreenPoint).direction;
        Vector3 level = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        Vector3 direction = Quaternion.FromToRotation(view.forward, level) * ray;
        return Quaternion.AngleAxis(keySide * keyTurnAngle, Vector3.up) * direction;
    }

    // Third person: point the nose at the direction under the cursor.
    private void Rotate()
    {
        // Aim direction in the body frame: how far the nose is off, up/down and sideways.
        Vector3 target = transform.InverseTransformDirection(CursorDirection());
        float pitchError = -Mathf.Atan2(target.y, target.z) * Mathf.Rad2Deg;  // + = nose must go down
        float yawError = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;     // + = aim is to the right

        Vector3 rate;   // deg/s
        rate.x = Mathf.Clamp(pitchError * pitchResponse, -maxPitchRate, maxPitchRate);
        rate.y = Mathf.Clamp(yawError * yawResponse, -maxYawRate, maxYawRate);
        float bank = Mathf.Clamp(yawError * bankPerDegree, -maxBank, maxBank);
        rate.z = Mathf.Abs(roll) > 0.01f ? -roll * rollRate : BankRate(bank, Vector3.up);

        ApplyRate(rate);
    }

    // First person: the mouse has turned the aim (about the world vertical, and
    // about its own wings - so pulling through the vertical loops); the nose snaps
    // onto it at full speed. Once the mouse rests, the aim's wings ease back to the
    // horizon - away from the vertical, where "level" means nothing.
    private void RotateAiming()
    {
        float dt = Time.fixedDeltaTime;
        aim = Quaternion.AngleAxis(aimLook.x, Vector3.up) * aim * Quaternion.Euler(-aimLook.y, 0f, 0f);
        aimRestTime = aimLook == Vector2.zero ? aimRestTime + dt : 0f;
        aimLook = Vector2.zero;

        Vector3 forward = aim * Vector3.forward;
        if (aimRestTime > aimLevelDelay && Mathf.Abs(forward.y) < 0.85f)
            aim = Quaternion.RotateTowards(aim, Quaternion.LookRotation(forward, Vector3.up), aimLevelRate * dt);

        Vector3 target = transform.InverseTransformDirection(aim * Vector3.forward);
        float pitchError = -Mathf.Atan2(target.y, target.z) * Mathf.Rad2Deg;
        float yawError = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;

        Vector3 rate;   // deg/s
        rate.x = Mathf.Clamp(pitchError * aimHoldResponse, -aimMaxRate, aimMaxRate);
        rate.y = Mathf.Clamp(yawError * aimHoldResponse, -aimMaxRate, aimMaxRate);
        rate.z = Mathf.Abs(roll) > 0.01f ? -roll * rollRate : BankRate(0f, aim * Vector3.up);

        ApplyRate(rate);
    }

    private void ApplyRate(Vector3 rate)
    {
        Vector3 current = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 accel = (rate * Mathf.Deg2Rad - current) * rateResponse;
        rb.AddRelativeTorque(accel, ForceMode.Acceleration);
    }

    // Roll toward "up", banked into the turn at speed: bank + = right wing down.
    // Hovering it turns flat. After a barrel roll this is what brings the wings
    // back. Nose straight up or down against a world "up", level means nothing:
    // hold the roll.
    private float BankRate(float bank, Vector3 up)
    {
        Vector3 forward = transform.forward;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.95f)
            return 0f;

        float atSpeed = Mathf.Clamp01(rb.linearVelocity.magnitude / bankSpeed);
        Vector3 wantedUp = Quaternion.AngleAxis(-bank * atSpeed, forward) * up;

        float error = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(transform.up, forward),
            Vector3.ProjectOnPlane(wantedUp, forward),
            forward);
        return Mathf.Clamp(error * rollResponse, -rollRate, rollRate);
    }

    // Push toward the wanted velocity: along the nose at the throttle's speed,
    // plus straight up/down from Space/Ctrl. Throttle zero and no lift = hover.
    // Aiming, the wanted velocity is what the keys ask for - forward/back and
    // sideways on the level, up/down - so with no keys the drone eases to a stop
    // with the same inertia limits as any other change of speed.
    private void Fly()
    {
        Vector3 forward = transform.forward;
        Vector3 wanted = Vector3.up * (lift * climbSpeed);
        if (aiming)
        {
            // Nose near vertical: the top of the body tells the heading instead.
            Vector3 level = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (level.sqrMagnitude < 0.01f)
                level = Vector3.ProjectOnPlane(transform.up * -Mathf.Sign(forward.y), Vector3.up);
            level.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, level);
            wanted += Vector3.ClampMagnitude(level * keyMove + side * keySide, 1f) * aimMoveSpeed;
        }
        else
        {
            wanted += forward * (boost ? boostSpeed : throttle * cruiseSpeed);
        }

        Vector3 error = wanted - rb.linearVelocity;
        Vector3 along = forward * Vector3.Dot(error, forward);
        Vector3 across = error - along;

        Vector3 accel = Vector3.ClampMagnitude(along * speedResponse, maxAcceleration)
                      + Vector3.ClampMagnitude(across * gripResponse, maxGrip);

        // The engine carries the weight; gravity still acts, so collisions and
        // the accelerations above stay physical.
        rb.AddForce(accel - Physics.gravity, ForceMode.Acceleration);
    }
}
