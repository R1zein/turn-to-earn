using UnityEngine;
using Zenject;

// War Thunder / WoT style "point-to-fly" drone control.
//
// Control model:
//  - The cursor drives an on-screen aim reticle (desired direction; drawn by DroneHudController).
//  - ROLL only banks the drone around its own forward axis. It does NOT turn it.
//  - PITCH is the only thing that rotates forward => it is what actually turns the drone.
//  - Cursor DIRECTION -> which way to bank (roll orients the pitch plane).
//  - Cursor DISTANCE from center -> how hard to pitch => turn speed.
//  - Upper half: pull (nose toward belly-up). Lower half: the logic mirrors -
//    roll flips to the opposite side and pitch pushes the nose DOWN.
//  - Center dead-zone: no roll, no pitch, drone levels out and holds course.
//  - Steering only works while the drone is actually moving.
//
// Sign notes: banking/pitch polarity depends on your rig. If an axis feels
// inverted in play, flip the sign on the marked line - nothing else changes.
[RequireComponent(typeof(Rigidbody))]
public class DroneControl : MonoBehaviour
{
    [Header("Steering")]
    [SerializeField] private float deadZone = 0.08f;   // normalized radius of the round dead zone, 0..1
    [SerializeField] private float rollSpeed = 180f;   // how fast we bank toward target (deg/s)
    [SerializeField] private float pitchRate = 90f;    // nose rotation speed at full deflection (deg/s)

    [Header("Movement")]
    [SerializeField] private float thrust = 20f;
    [SerializeField] private float deceleration = 0.5f;
    [SerializeField] private float moveThreshold = 0.5f; // min speed (m/s) required to steer
    [Tooltip("How fast the velocity turns toward the nose, deg/s. 0 = free drift: the drone slides on the old heading after a turn")]
    [SerializeField] private float velocityFollow = 180f;

    // Throttle and the cursor position. The cursor itself (confined, hidden - we draw
    // our own reticle) is the scene's base InputMode.Drone.
    [Inject] private InputService input;

    private Rigidbody rb;
    private DroneShooting shooting;

    // |forward.y| above this = nose within ~2.5 deg of straight up/down, where bank
    // relative to the horizon is undefined.
    private const float NearVerticalNose = 0.999f;

    private Vector2 aim;     // cursor offset from screen center, dead-zoned, -1..1 per axis
    private bool throttle;
    private bool isPushing;  // last PULL/PUSH choice, kept while the bank is undefined

    private Vector3 lastPosition;
    private float distanceTravelled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();

        lastPosition = rb.position;
    }

    private void Update()
    {
        ReadInput();
        Steer();

        if (shooting != null)
            shooting.Shoot();
    }

    private void FixedUpdate()
    {
        Move();
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
        throttle = input.IsThrottleHeld;

        Vector2 mouse = input.DroneAim;
        Vector2 half = new Vector2(Screen.width, Screen.height) * 0.5f;

        // offset from screen center in units of half the screen HEIGHT on both axes, so
        // the same mouse travel gives the same deflection in every direction (dividing x
        // by half the width made sideways turns need twice the travel on a wide screen).
        // Full deflection is a circle of radius half-height; clamping the length keeps
        // the direction intact.
        Vector2 raw = Vector2.ClampMagnitude((mouse - half) / half.y, 1f);

        // round dead-zone in the center: inside it we hold course. Round, not per-axis:
        // zeroing each axis on its own snapped the direction onto an axis near it
        // (a cursor slightly right of straight up read as exactly up).
        aim = raw.magnitude < deadZone ? Vector2.zero : raw;
    }

    private void Steer()
    {
        // "only in motion": nothing happens while (nearly) stationary
        if (rb.linearVelocity.magnitude < moveThreshold)
            return;

        // With the nose straight up or down the bank is undefined: don't roll and don't
        // re-pick PULL/PUSH there, or the reading flickers 0/180 and the drone spins
        // about its nose.
        bool hasBank = TryGetRoll(out float currentRoll);

        // distance from center = maneuver strength (turn rate)
        float d = Mathf.Clamp01(aim.magnitude);

        // Centered: no maneuver. Level the wings (recovers from any bank/roll,
        // including a full barrel) and hold course.
        if (d < 1e-4f)
        {
            if (!hasBank)
                return;

            float level = Mathf.MoveTowards(currentRoll, 0f, rollSpeed * Time.deltaTime) - currentRoll;
            transform.Rotate(0f, 0f, level, Space.Self);
            return;
        }

        // Direction the cursor points, in screen space: 0 = up, +90 = right, 180 = down.
        float phi = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;

        // Two ways to send the nose toward the cursor, 180 deg apart in bank:
        //   PULL: bank so belly-up faces the cursor, then pitch the nose UP.
        //   PUSH: bank the opposite way, then pitch the nose DOWN.
        // Pick whichever bank is CLOSER to the current one. This gives both wanted
        // behaviours from a single rule:
        //   - sweeping the cursor gradually keeps the same solution, so the drone
        //     keeps rolling the same way past 90 deg (a barrel roll);
        //   - a sudden jump (e.g. neutral -> straight down) finds the shorter
        //     solution: opposite bank + nose down, instead of a 180 flip.
        float pullZ = -phi;          // bank (euler z) for the pull solution
        float pushZ = -phi + 180f;   // bank for the push solution

        if (hasBank)
        {
            float distPull = Mathf.Abs(Mathf.DeltaAngle(currentRoll, pullZ));
            float distPush = Mathf.Abs(Mathf.DeltaAngle(currentRoll, pushZ));
            isPushing = distPush < distPull;    // prefer pull on ties
        }
        bool push = isPushing;
        float targetZ = push ? pushZ : pullZ;
        float pitchSign = push ? -1f : 1f;  // push => nose down, pull => nose up

        // ROLL banks toward the chosen target by the shortest path. Using the wrapped
        // delta lets a barrel roll flow smoothly through +/-180 without snapping.
        float rollDelta = hasBank ? Mathf.DeltaAngle(currentRoll, targetZ) : 0f;  // <- negate for inverted bank

        // PITCH continuously rotates forward => the actual turn. Strength = distance,
        // scaled by how far the bank has got: pitching while still rolling turns the
        // nose in the old plane (a hard right swing became a 33 deg climb). cos of the
        // remaining bank error is the share of the pitch that goes the wanted way.
        float alignment = Mathf.Max(0f, Mathf.Cos(rollDelta * Mathf.Deg2Rad));
        transform.Rotate(-pitchSign * d * alignment * pitchRate * Time.deltaTime, 0f, 0f, Space.Self); // <- flip sign if pitch is inverted

        rollDelta = Mathf.Clamp(rollDelta, -rollSpeed * Time.deltaTime, rollSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, rollDelta, Space.Self);
    }

    private void Move()
    {
        // The velocity swings toward the nose, keeping its length: in a turn the drone
        // flies where it points instead of skidding outward (11.8 deg average slip in a
        // full-deflection turn with throttle before this).
        Vector3 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 1e-4f)
        {
            float maxTurn = velocityFollow * Mathf.Deg2Rad * Time.fixedDeltaTime;
            rb.linearVelocity = Vector3.RotateTowards(velocity, transform.forward * velocity.magnitude, maxTurn, 0f);
        }

        if (throttle)
        {
            // move strictly along the nose (no roll-based drift)
            rb.linearVelocity += transform.forward * thrust * Time.fixedDeltaTime;
        }

        rb.linearVelocity *= 1f - deceleration * Time.fixedDeltaTime;
    }

    // Bank about the nose, measured from "level up" - world up projected onto the
    // plane across the nose. In normal flight this equals eulerAngles.z, but it has no
    // gimbal-lock band near vertical (where eulerAngles.z reads 0 whatever the bank).
    private bool TryGetRoll(out float roll)
    {
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.y) > NearVerticalNose)
        {
            roll = 0f;
            return false;
        }

        Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, forward);
        roll = Vector3.SignedAngle(levelUp, transform.up, forward);
        return true;
    }
}
