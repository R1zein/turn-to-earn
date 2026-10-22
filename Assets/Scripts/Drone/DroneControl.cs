using UnityEngine;
using Zenject;

// Arcade plane in the spirit of Fortnite's: it flies where the nose points at the
// speed the throttle sets, never stalls, and with the throttle at zero it brakes
// and hangs in the air like a VTOL. Physics stays honest about inertia: the
// rotors/engine push toward a WANTED velocity with limited acceleration - fast
// along the nose, firmer sideways ("grip") - so a hard turn at speed drifts a
// little. Gravity is on and the engine always carries the weight.
//
// The mouse moves an invisible on-screen CURSOR (the HUD ring is drawn on it); an
// autopilot points the nose where the cursor says, the way a pilot would: pitch
// and a little rudder toward it, and a bank into the turn. The chase camera sits
// behind the drone's heading, so the screen centre (the static diamond) means
// "straight and level": a cursor left to the side keeps the drone turning, a
// cursor above the centre holds a climb at that angle. Q/E take the roll over - hold
// for barrel rolls; released, the autopilot rolls back level. A/D steer while
// held. Rotation always goes through torque: a body-rate controller turns
// commanded rates into angular acceleration.
//
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

    [Header("Inertia")]
    [Tooltip("1/s: how fast the speed along the nose reaches the wanted one")]
    [SerializeField] private float speedResponse = 1.2f;
    [Tooltip("Acceleration/braking limit along the nose, m/s^2")]
    [SerializeField] private float maxAcceleration = 20f;
    [Tooltip("1/s: how fast sideways/vertical slip is killed - the wings' grip")]
    [SerializeField] private float gripResponse = 6f;
    [Tooltip("Sideways acceleration limit, m/s^2 (~9 g - a full-rate loop at cruise speed needs 8.5): beyond it a turn drifts")]
    [SerializeField] private float maxGrip = 90f;

    [Header("Autopilot")]
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

    // Kept as an offset from the screen centre: the centre stays exact even if the
    // screen size changes after Awake (it does in the editor), or the window is resized.
    private Vector2 cursorOffset;
    private float throttle;
    private float roll;     // Q/E: -1 left .. 1 right
    private float lift;     // Space/Ctrl: -1 down .. 1 up
    private bool boost;

    private Vector3 lastPosition;
    private float distanceTravelled;

    // The invisible cursor the pilot steers with, in screen pixels (origin bottom-left).
    public Vector2 AimScreenPoint => new Vector2(Screen.width, Screen.height) * 0.5f + cursorOffset;
    public float Throttle => throttle;
    public bool IsBoosting => boost;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();
        // Default 7 rad/s (400 deg/s) would clip fast rolls.
        rb.maxAngularVelocity = 20f;

        lastPosition = rb.position;
    }

    private void Update()
    {
        ReadInput();
        MoveCursor();

        if (shooting != null)
            shooting.Shoot();
    }

    private void FixedUpdate()
    {
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
        throttle = Mathf.Clamp01(throttle + input.DroneThrottle * throttleRate * Time.deltaTime);
        roll = input.DroneRoll;
        lift = input.DroneLift;
        boost = input.IsBoostHeld;
    }

    // The cursor moves with the mouse 1:1 (times DroneCursorSpeed) and stays where
    // it was left, like a normal cursor - it is only invisible.
    private void MoveCursor()
    {
        Vector2 half = new Vector2(Screen.width, Screen.height) * 0.5f - Vector2.one * cursorMargin;
        cursorOffset += input.DroneLook * config.DroneCursorSpeed;
        cursorOffset.x = Mathf.Clamp(cursorOffset.x, -half.x, half.x);
        cursorOffset.y = Mathf.Clamp(cursorOffset.y, -half.y, half.y);
    }

    // Direction under the cursor, measured from the screen centre rather than from
    // the camera's slight look-down: the centre means level flight along the
    // camera's heading. Swung sideways while A/D are held.
    private Vector3 AimDirection()
    {
        Transform view = sceneCamera.transform;
        Vector3 ray = sceneCamera.ScreenPointToRay(AimScreenPoint).direction;
        Vector3 level = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        Vector3 direction = Quaternion.FromToRotation(view.forward, level) * ray;
        return Quaternion.AngleAxis(input.DroneYaw * keyTurnAngle, Vector3.up) * direction;
    }

    private void Rotate()
    {
        // Aim direction in the body frame: how far the nose is off, up/down and sideways.
        Vector3 target = transform.InverseTransformDirection(AimDirection());
        float pitchError = -Mathf.Atan2(target.y, target.z) * Mathf.Rad2Deg;  // + = nose must go down
        float yawError = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;     // + = aim is to the right

        Vector3 rate;   // deg/s
        rate.x = Mathf.Clamp(pitchError * pitchResponse, -maxPitchRate, maxPitchRate);
        rate.y = Mathf.Clamp(yawError * yawResponse, -maxYawRate, maxYawRate);
        rate.z = Mathf.Abs(roll) > 0.01f ? -roll * rollRate : BankRate(yawError);

        Vector3 current = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 accel = (rate * Mathf.Deg2Rad - current) * rateResponse;
        rb.AddRelativeTorque(accel, ForceMode.Acceleration);
    }

    // Roll toward the horizon, banked into the turn at speed: aim to the right =
    // right wing down. Hovering it turns flat. After a barrel roll this is what
    // brings the wings back level. Nose straight up or down, "level" means
    // nothing: hold the roll.
    private float BankRate(float yawError)
    {
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.y) > 0.95f)
            return 0f;

        float atSpeed = Mathf.Clamp01(rb.linearVelocity.magnitude / bankSpeed);
        float bank = Mathf.Clamp(yawError * bankPerDegree, -maxBank, maxBank) * atSpeed;
        Vector3 wantedUp = Quaternion.AngleAxis(-bank, forward) * Vector3.up;

        float error = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(transform.up, forward),
            Vector3.ProjectOnPlane(wantedUp, forward),
            forward);
        return Mathf.Clamp(error * rollResponse, -rollRate, rollRate);
    }

    // Push toward the wanted velocity: along the nose at the throttle's speed,
    // plus straight up/down from Space/Ctrl. Throttle zero and no lift = hover.
    private void Fly()
    {
        Vector3 forward = transform.forward;
        float speed = boost ? boostSpeed : throttle * cruiseSpeed;
        Vector3 wanted = forward * speed + Vector3.up * (lift * climbSpeed);

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
