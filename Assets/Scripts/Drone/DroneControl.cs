using UnityEngine;
using Zenject;

// Arcade plane in the spirit of Fortnite's: it flies where the nose points at the
// speed the throttle sets, never stalls, and with the throttle at zero it brakes
// and hangs in the air like a VTOL. Physics stays honest about inertia: the
// rotors/engine push toward a WANTED velocity with limited acceleration - fast
// along the nose, firmer sideways ("grip") - so a hard turn at speed drifts a
// little. Gravity is on and the engine always carries the weight.
//
// Steering is a MOUSE JOYSTICK. The mouse moves an invisible on-screen cursor
// (the HUD's cyan ring); its offset from the screen centre is a deflection:
//  - inside the dead zone around the centre (the ring around the red diamond)
//    nothing turns - the drone holds course and pitch;
//  - beyond it the drone turns right/left about the world vertical and pitches
//    up/down, faster the further out, measured from the dead-zone edge so there
//    is no jump at the boundary. Holding the cursor up makes a loop.
// The stick turns a TARGET attitude, and the nose is held on it: the stick's
// rates are fed forward and any error is corrected, so inside the dead zone the
// drone holds exactly - turning with bank used to let the nose sag a few degrees
// per turn, enough to fly a level cruise into the ground. Once the stick rests,
// the target's wings ease back to the horizon (after half a loop this rolls the
// drone upright - an Immelmann).
// The cursor glides back to the centre once the mouse rests (auto-centre, C
// toggles it): flick to turn, let go to straighten. A/D add to the sideways
// deflection, Q/E take the roll over (barrel rolls); otherwise the wings bank
// into the turn at speed and level out after. Rotation always goes through
// torque: a body-rate controller turns commanded rates into angular acceleration.
//
// Holding Aim (RMB) is the sniping stance: the camera moves to the nose (first
// person, DroneCamera) and the mouse stops being a joystick - like FPS mouse-look
// it turns the target attitude directly (GameConfig.DroneAimSensitivity deg per
// pixel) and the nose snaps onto it at full speed, so no dead zone is needed.
// The drone brakes to a hover and W/S/A/D/Space/Ctrl move it directly -
// forward/back, sideways, up/down - like a quadcopter. The throttle is kept and
// takes over again on release.
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

    [Header("Mouse joystick")]
    [Tooltip("Dead-zone radius around the centre, in screen heights: inside it nothing turns")]
    [SerializeField] private float deadZone = 0.05f;
    [Tooltip("Cursor offset for full deflection, in screen heights")]
    [SerializeField] private float fullDeflection = 0.35f;
    [Tooltip("Turn rate at full deflection, deg/s")]
    [SerializeField] private float maxYawRate = 90f;
    [Tooltip("Pitch rate at full deflection, deg/s")]
    [SerializeField] private float maxPitchRate = 110f;
    [Tooltip("The cursor stays this many pixels inside the screen edge")]
    [SerializeField] private float cursorMargin = 24f;

    [Tooltip("1/s: how hard the nose is pulled back onto the target attitude")]
    [SerializeField] private float holdResponse = 4f;
    [SerializeField] private float maxHoldRate = 120f;
    [Tooltip("Aiming: how hard the nose snaps onto the target, 1/s")]
    [SerializeField] private float aimHoldResponse = 15f;
    [Tooltip("Aiming: fastest the nose turns, deg/s")]
    [SerializeField] private float aimMaxRate = 360f;
    [Tooltip("Seconds of a resting stick before the target's wings level with the horizon")]
    [SerializeField] private float levelDelay = 0.4f;
    [Tooltip("How fast they level, deg/s")]
    [SerializeField] private float levelRate = 90f;

    [Header("Cursor auto-centre")]
    [Tooltip("Seconds of a resting mouse before the cursor starts back to the centre")]
    [SerializeField] private float autoCentreDelay = 0.35f;
    [Tooltip("1/s: how fast it glides back (3 = about a second)")]
    [SerializeField] private float autoCentreRate = 3f;

    [Header("Bank and roll")]
    [Tooltip("Bank at full sideways deflection, at speed, deg")]
    [SerializeField] private float maxBank = 60f;
    [Tooltip("Below this speed the drone turns flat (hovering), m/s")]
    [SerializeField] private float bankSpeed = 15f;
    [Tooltip("1/s: bank error (deg) -> roll rate")]
    [SerializeField] private float rollResponse = 6f;
    [Tooltip("Q/E roll rate, deg/s")]
    [SerializeField] private float rollRate = 270f;
    [Tooltip("1/s: how fast the body reaches a commanded rate")]
    [SerializeField] private float rateResponse = 12f;

    [Inject] private InputService input;
    [Inject] private GameConfig config;

    private Rigidbody rb;
    private DroneShooting shooting;

    // Kept as an offset from the screen centre: the centre stays exact even if the
    // screen size changes after Awake (it does in the editor), or the window is resized.
    private Vector2 cursorOffset;
    private float mouseRestTime;
    private Quaternion target;   // the attitude the stick steers; the nose is held on it
    private float stickRestTime;
    private Vector2 aimLook;     // aiming: mouse turn not yet applied to the target, deg
    private bool autoCentre = true;
    private float throttle;
    private float roll;     // Q/E: -1 left .. 1 right
    private float lift;     // Space/Ctrl: -1 down .. 1 up
    private float keyMove;  // W/S while aiming: -1 back .. 1 forward
    private float keySide;  // A/D: -1 left .. 1 right
    private bool boost;
    private bool aiming;

    private Vector3 lastPosition;
    private float distanceTravelled;

    // The invisible cursor the pilot steers with, in screen pixels (origin bottom-left).
    public Vector2 AimScreenPoint => new Vector2(Screen.width, Screen.height) * 0.5f + cursorOffset;
    // Dead-zone radius around the screen centre, in screen pixels.
    public float DeadZoneRadius => deadZone * Screen.height;
    public bool IsAutoCentring => autoCentre;
    // Where the target attitude points: while aiming, the mouse's direction.
    public Vector3 AimDirection => target * Vector3.forward;
    public float Throttle => throttle;
    public bool IsBoosting => boost;
    public bool IsAiming => aiming;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();
        // Default 7 rad/s (400 deg/s) would clip fast rolls.
        rb.maxAngularVelocity = 20f;

        target = Quaternion.LookRotation(transform.forward, Vector3.up);
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
        aiming = input.IsDroneAimHeld;
        // While aiming W/S move the drone instead of setting the throttle.
        keyMove = aiming ? input.DroneThrottle : 0f;
        if (!aiming)
            throttle = Mathf.Clamp01(throttle + input.DroneThrottle * throttleRate * Time.deltaTime);
        keySide = input.DroneYaw;
        roll = input.DroneRoll;
        lift = input.DroneLift;
        boost = input.IsBoostHeld;
        if (input.DroneAutoCentreToggled)
            autoCentre = !autoCentre;
    }

    // The cursor moves with the mouse 1:1 (times DroneCursorSpeed). Once the mouse
    // has rested for autoCentreDelay it glides back to the centre, if auto-centre is on.
    private void MoveCursor()
    {
        Vector2 look = input.DroneLook;
        // Aiming, the mouse turns the target directly; the cursor waits in the centre.
        if (aiming)
        {
            aimLook += look * config.DroneAimSensitivity;
            cursorOffset = Vector2.zero;
            return;
        }

        mouseRestTime = look.sqrMagnitude > 0f ? 0f : mouseRestTime + Time.deltaTime;

        cursorOffset += look * config.DroneCursorSpeed;
        if (autoCentre && mouseRestTime > autoCentreDelay)
            cursorOffset *= Mathf.Exp(-autoCentreRate * Time.deltaTime);

        Vector2 half = new Vector2(Screen.width, Screen.height) * 0.5f - Vector2.one * cursorMargin;
        cursorOffset.x = Mathf.Clamp(cursorOffset.x, -half.x, half.x);
        cursorOffset.y = Mathf.Clamp(cursorOffset.y, -half.y, half.y);
    }

    // Cursor offset as a stick deflection, -1..1 per axis: zero inside the dead
    // zone, growing from its edge to full at fullDeflection.
    private Vector2 Deflection()
    {
        float radius = cursorOffset.magnitude;
        float dead = deadZone * Screen.height;
        if (radius <= dead)
            return Vector2.zero;

        float full = fullDeflection * Screen.height;
        float strength = Mathf.Clamp01((radius - dead) / (full - dead));
        return cursorOffset / radius * strength;
    }

    private void Rotate()
    {
        float dt = Time.fixedDeltaTime;
        // Aiming: no joystick - the mouse has turned the target directly (aimLook).
        Vector2 stick = aiming ? Vector2.zero : Deflection();
        // A/D steer like the cursor - except while aiming, where they strafe.
        float turn = Mathf.Clamp(stick.x + (aiming ? 0f : keySide), -1f, 1f);
        float yawRate = turn * maxYawRate;
        float pitchRate = stick.y * maxPitchRate;   // + = nose up

        // The target turns about the world vertical and pitches about its own wings,
        // so pulling through the vertical makes a loop.
        float yawStep = yawRate * dt + aimLook.x;
        float pitchStep = pitchRate * dt + aimLook.y;
        target = Quaternion.AngleAxis(yawStep, Vector3.up) * target * Quaternion.Euler(-pitchStep, 0f, 0f);
        LevelTarget(yawStep != 0f || pitchStep != 0f, dt);
        aimLook = Vector2.zero;

        // Feed the stick's rates forward, then pull the nose onto the target.
        Vector3 worldRate = Vector3.up * yawRate - (target * Vector3.right) * pitchRate;
        Vector3 rate = transform.InverseTransformDirection(worldRate);  // deg/s

        Vector3 aim = transform.InverseTransformDirection(target * Vector3.forward);
        float pitchError = -Mathf.Atan2(aim.y, aim.z) * Mathf.Rad2Deg;  // + = nose must go down
        float yawError = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;     // + = target is to the right
        float hold = aiming ? aimHoldResponse : holdResponse;
        float maxHold = aiming ? aimMaxRate : maxHoldRate;
        rate.x += Mathf.Clamp(pitchError * hold, -maxHold, maxHold);
        rate.y += Mathf.Clamp(yawError * hold, -maxHold, maxHold);
        rate.z = Mathf.Abs(roll) > 0.01f ? -roll * rollRate : BankRate(turn);

        Vector3 current = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 accel = (rate * Mathf.Deg2Rad - current) * rateResponse;
        rb.AddRelativeTorque(accel, ForceMode.Acceleration);
    }

    // Once the stick has rested a moment, ease the target's wings back to the
    // horizon - away from the vertical, where "level" means nothing.
    private void LevelTarget(bool isSteering, float dt)
    {
        stickRestTime = isSteering ? 0f : stickRestTime + dt;
        Vector3 forward = target * Vector3.forward;
        if (stickRestTime > levelDelay && Mathf.Abs(forward.y) < 0.85f)
            target = Quaternion.RotateTowards(target, Quaternion.LookRotation(forward, Vector3.up), levelRate * dt);
    }

    // Roll toward the target's wings, banked into the turn at speed: turning right =
    // right wing down. Hovering it turns flat. After a barrel roll this is what
    // brings the wings back.
    private float BankRate(float turn)
    {
        Vector3 forward = transform.forward;
        float atSpeed = Mathf.Clamp01(rb.linearVelocity.magnitude / bankSpeed);
        float bank = turn * maxBank * atSpeed;
        Vector3 wantedUp = Quaternion.AngleAxis(-bank, forward) * (target * Vector3.up);

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
