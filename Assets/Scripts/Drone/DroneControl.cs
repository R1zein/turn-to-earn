using UnityEngine;
using Zenject;

// FPV quadcopter. The only lift is rotor thrust along the body's UP axis; gravity,
// drag and inertia do the rest, so tilting the body is what makes it travel.
// Rotation always goes through torque: a rate controller (like a real flight
// controller) turns commanded body rates into angular acceleration.
//
// Two flight modes over the same physics:
//  - STAB (default): W/S and A/D tilt the drone to an angle, releasing them
//    levels it; neutral thrust holds altitude whatever the tilt.
//  - ACRO (hold Acro, or toggle it): the keys command rotation RATES and nothing
//    levels - hold A/D for a barrel roll, W/S for a flip. Neutral thrust is hover
//    thrust scaled by how upright the drone is, so a flip is a ballistic arc
//    rather than a dive driven by the rotors.
//
// Body axes in Unity: +X rate = nose down, +Y = yaw right, +Z = roll left.
[RequireComponent(typeof(Rigidbody))]
public class DroneControl : MonoBehaviour
{
    [Header("Rates, deg/s")]
    [Tooltip("Pitch and roll rate in ACRO: 360 = a full flip or roll per second")]
    [SerializeField] private float acroRate = 360f;
    [SerializeField] private float yawRate = 160f;
    [Tooltip("1/s: how fast the body reaches a commanded rate")]
    [SerializeField] private float rateResponse = 12f;

    [Header("STAB mode")]
    [SerializeField] private float maxTilt = 30f;
    [Tooltip("Tilt with Boost held: more tilt = more speed")]
    [SerializeField] private float boostTilt = 50f;
    [Tooltip("1/s: attitude error (deg) -> rate")]
    [SerializeField] private float levelResponse = 6f;
    [SerializeField] private float maxLevelRate = 300f;

    [Header("STAB altitude hold")]
    [Tooltip("Climb/descent speed with Lift held, m/s; released, vertical speed goes to zero")]
    [SerializeField] private float climbSpeed = 10f;
    [Tooltip("1/s: how hard the rotors chase the wanted vertical speed")]
    [SerializeField] private float climbResponse = 4f;

    [Header("Thrust, in g")]
    [Tooltip("Full rotor power: the STAB limit, and ACRO with Boost")]
    [SerializeField] private float maxThrust = 2.5f;
    [Tooltip("ACRO with Lift up")]
    [SerializeField] private float climbThrust = 1.8f;

    [Header("Keys")]
    [Tooltip("1/s: a key ramps the stick to full deflection in 1/value seconds")]
    [SerializeField] private float stickResponse = 8f;

    [Inject] private InputService input;

    private Rigidbody rb;
    private DroneShooting shooting;

    // Smoothed sticks, -1..1: pitch + = nose down, roll + = right, yaw + = right, lift + = up.
    private float pitch, roll, yaw, lift;
    private bool boost;
    private bool acroLatched;

    private Vector3 lastPosition;
    private float distanceTravelled;

    public bool IsAcro => acroLatched || input.IsAcroHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();
        // Default 7 rad/s (400 deg/s) would clip a 360 deg/s roll plus overshoot.
        rb.maxAngularVelocity = 20f;

        lastPosition = rb.position;
    }

    private void Update()
    {
        ReadInput();

        if (shooting != null)
            shooting.Shoot();
    }

    private void FixedUpdate()
    {
        Rotate();
        Thrust();
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
        if (input.AcroToggled)
            acroLatched = !acroLatched;

        float step = stickResponse * Time.deltaTime;
        pitch = Mathf.MoveTowards(pitch, input.DronePitch, step);
        roll = Mathf.MoveTowards(roll, input.DroneRoll, step);
        yaw = Mathf.MoveTowards(yaw, input.DroneYaw, step);
        lift = Mathf.MoveTowards(lift, input.DroneLift, step);
        boost = input.IsBoostHeld;
    }

    private void Rotate()
    {
        // Commanded body rates, deg/s.
        Vector3 rate = IsAcro
            ? new Vector3(pitch * acroRate, 0f, -roll * acroRate)
            : LevelingRate();
        rate.y = yaw * yawRate;

        Vector3 current = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 accel = (rate * Mathf.Deg2Rad - current) * rateResponse;
        rb.AddRelativeTorque(accel, ForceMode.Acceleration);
    }

    // STAB: the rate that turns the body toward the attitude the sticks ask for -
    // level, or tilted by up to maxTilt - keeping the current heading. Built as a
    // rotation error, so it also rights the drone from upside down after ACRO.
    private Vector3 LevelingRate()
    {
        float tilt = boost ? boostTilt : maxTilt;
        Quaternion desired = Quaternion.LookRotation(Heading()) * Quaternion.Euler(pitch * tilt, 0f, -roll * tilt);

        (desired * Quaternion.Inverse(rb.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;
        if (float.IsInfinity(axis.x))   // no error at all
            return Vector3.zero;

        Vector3 error = transform.InverseTransformDirection(axis * angle);
        Vector2 pitchRoll = Vector2.ClampMagnitude(new Vector2(error.x, error.z) * levelResponse, maxLevelRate);
        return new Vector3(pitchRoll.x, 0f, pitchRoll.y);
    }

    // Horizontal direction the drone faces. With the nose near vertical the
    // forward axis says nothing about heading, but the top does: nose up, the top
    // faces backward; nose down, it faces forward.
    private Vector3 Heading()
    {
        Vector3 forward = transform.forward;
        Vector3 flat = new Vector3(forward.x, 0f, forward.z);
        if (flat.sqrMagnitude < 0.01f)
        {
            Vector3 top = transform.up * (forward.y > 0f ? -1f : 1f);
            flat = new Vector3(top.x, 0f, top.z);
        }
        return flat.normalized;
    }

    private void Thrust()
    {
        float g = -Physics.gravity.y;
        float upright = transform.up.y;
        float thrust;   // in g

        if (IsAcro)
        {
            // Raw rotors: hover power when level, less as the drone tips over,
            // none upside down - a flip is a ballistic arc. Lift and Boost add power.
            float hover = Mathf.Max(0f, upright);
            thrust = boost ? maxThrust
                : lift >= 0f ? Mathf.Lerp(hover, climbThrust, lift)
                : Mathf.Lerp(hover, 0f, -lift);
        }
        else if (upright > 0.1f)
        {
            // Altitude hold: carry the weight whatever the tilt and chase the
            // vertical speed Lift asks for (zero when released).
            float wantedVy = lift * climbSpeed;
            float upAccel = g + (wantedVy - rb.linearVelocity.y) * climbResponse;
            thrust = Mathf.Clamp(upAccel / upright / g, 0f, maxThrust);
        }
        else
        {
            // Upside down while STAB rights it: the rotors would only push it down.
            thrust = 0f;
        }

        rb.AddForce(transform.up * (thrust * g), ForceMode.Acceleration);
    }
}
