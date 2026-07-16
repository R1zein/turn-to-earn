using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor; // UI Toolkit ships its own Cursor type

// War Thunder / WoT style "point-to-fly" drone control.
//
// Control model:
//  - The cursor drives an on-screen aim reticle (desired direction).
//  - A second on-screen marker shows the drone's real forward (physical direction).
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
    [Header("UI (UI Toolkit, elements looked up by name in the HUD document)")]
    [SerializeField] private UIDocument hud;                      // panel hosting the reticle elements
    [SerializeField] private string aimReticleName = "Scope";     // desired direction, follows the cursor
    [SerializeField] private string headingIndicatorName;         // real forward, driven by code; empty = unused
    [SerializeField] private Camera viewCamera;                   // used to project forward onto screen
    [SerializeField] private float indicatorDistance = 200f;      // how far ahead forward is projected

    [Header("Steering")]
    [SerializeField] private float deadZone = 0.08f;   // normalized square half-size, 0..1
    [SerializeField] private float rollSpeed = 180f;   // how fast we bank toward target (deg/s)
    [SerializeField] private float pitchRate = 90f;    // nose rotation speed at full deflection (deg/s)

    [Header("Movement")]
    [SerializeField] private float thrust = 20f;
    [SerializeField] private float deceleration = 0.5f;
    [SerializeField] private float moveThreshold = 0.5f; // min speed (m/s) required to steer

    private Rigidbody rb;
    private DroneShooting shooting;

    private VisualElement aimReticle;
    private VisualElement headingIndicator;
    private bool hudResolved;

    private Vector2 aim;     // cursor offset from screen center, dead-zoned, -1..1 per axis
    private bool throttle;

    private Vector3 lastPosition;
    private float distanceTravelled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<DroneShooting>();
        if (viewCamera == null)
            viewCamera = Camera.main;

        lastPosition = rb.position;

        Cursor.lockState = CursorLockMode.Confined; // keep cursor in window but let it move freely
        Cursor.visible = false;                     // we draw our own reticle instead
    }

    private void Update()
    {
        ResolveHud();
        ReadInput();
        UpdateReticle();
        UpdateHeadingIndicator();
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
        throttle = Input.GetKey(KeyCode.LeftShift);

        Vector2 mouse = Input.mousePosition;
        Vector2 half = new Vector2(Screen.width, Screen.height) * 0.5f;

        // normalized offset from screen center, -1..1 per axis
        Vector2 raw = new Vector2((mouse.x - half.x) / half.x, (mouse.y - half.y) / half.y);
        raw.x = Mathf.Clamp(raw.x, -1f, 1f);
        raw.y = Mathf.Clamp(raw.y, -1f, 1f);

        // square dead-zone in the center: inside it we hold course
        aim = raw;
        if (Mathf.Abs(aim.x) < deadZone) aim.x = 0f;
        if (Mathf.Abs(aim.y) < deadZone) aim.y = 0f;
    }

    // The document builds its elements on its own OnEnable, which may land after ours,
    // so grab them on the first Update that finds a live root.
    private void ResolveHud()
    {
        if (hudResolved || hud == null)
            return;

        VisualElement root = hud.rootVisualElement;
        if (root == null)
            return;

        aimReticle = FindElement(root, aimReticleName);
        headingIndicator = FindElement(root, headingIndicatorName);
        hudResolved = true;
    }

    private static VisualElement FindElement(VisualElement root, string name)
    {
        return string.IsNullOrEmpty(name) ? null : root.Q<VisualElement>(name);
    }

    // Desired-direction reticle simply tracks the cursor.
    private void UpdateReticle()
    {
        if (aimReticle != null)
            PlaceAtScreenPoint(aimReticle, Input.mousePosition);
    }

    // Physical-direction marker = drone forward projected onto the screen.
    private void UpdateHeadingIndicator()
    {
        if (headingIndicator == null || viewCamera == null)
            return;

        Vector3 world = transform.position + transform.forward * indicatorDistance;
        Vector3 screen = viewCamera.WorldToScreenPoint(world);

        bool inFront = screen.z > 0f;
        headingIndicator.style.display = inFront ? DisplayStyle.Flex : DisplayStyle.None;
        if (inFront)
            PlaceAtScreenPoint(headingIndicator, screen);
    }

    // Screen space has Y running up from the bottom, the panel has it running down from
    // the top, hence the flip. ScreenToPanel then undoes the panel's own scaling.
    // The elements carry translate: -50% -50%, so left/top place their centre on the point.
    private static void PlaceAtScreenPoint(VisualElement element, Vector2 screenPoint)
    {
        IPanel panel = element.panel;
        if (panel == null)
            return;

        Vector2 local = RuntimePanelUtils.ScreenToPanel(
            panel, new Vector2(screenPoint.x, Screen.height - screenPoint.y));

        element.style.left = local.x;
        element.style.top = local.y;
    }

    private void Steer()
    {
        // "only in motion": nothing happens while (nearly) stationary
        if (rb.linearVelocity.magnitude < moveThreshold)
            return;

        float currentRoll = GetSignedAngle(transform.eulerAngles.z);

        // distance from center = maneuver strength (turn rate)
        float d = Mathf.Clamp01(aim.magnitude);

        // Centered: no maneuver. Level the wings (recovers from any bank/roll,
        // including a full barrel) and hold course.
        if (d < 1e-4f)
        {
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

        float distPull = Mathf.Abs(Mathf.DeltaAngle(currentRoll, pullZ));
        float distPush = Mathf.Abs(Mathf.DeltaAngle(currentRoll, pushZ));

        bool push = distPush < distPull;    // prefer pull on ties
        float targetZ = push ? pushZ : pullZ;
        float pitchSign = push ? -1f : 1f;  // push => nose down, pull => nose up

        // PITCH continuously rotates forward => the actual turn. Strength = distance.
        transform.Rotate(-pitchSign * d * pitchRate * Time.deltaTime, 0f, 0f, Space.Self); // <- flip sign if pitch is inverted

        // ROLL banks toward the chosen target by the shortest path. Using the wrapped
        // delta lets a barrel roll flow smoothly through +/-180 without snapping.
        float rollDelta = Mathf.DeltaAngle(currentRoll, targetZ);            // <- negate for inverted bank
        rollDelta = Mathf.Clamp(rollDelta, -rollSpeed * Time.deltaTime, rollSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, rollDelta, Space.Self);
    }

    private void Move()
    {
        if (throttle)
        {
            // move strictly along the nose (no roll-based drift)
            rb.linearVelocity += transform.forward * thrust * Time.fixedDeltaTime;
        }

        rb.linearVelocity *= 1f - deceleration * Time.fixedDeltaTime;
    }

    private static float GetSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
