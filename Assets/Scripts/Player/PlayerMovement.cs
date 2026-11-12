using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

// The player's legs: a capsule on a Rigidbody that steers its own velocity.
//
// Each physics step it works out the plane it stands on and moves the velocity
// *within that plane* toward what the keys ask for. Everything else follows from
// that one rule:
//  - the ground is whatever the capsule touches from below, on any layer — the
//    contacts of the last step, not a ray against a "ground" layer;
//  - a bump or the top of a ramp does not launch the body: the step it loses the
//    ground it is turned back along the surface underneath (FindGround);
//  - gravity is applied by hand, so on a walkable slope it presses into the ground
//    instead of dragging the body downhill, and in the air it is heavier than g,
//    which is what gives the jump its weight.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    // Physics steps after a jump during which the ground is ignored: the contact
    // with the floor outlives the takeoff by a step or two and would otherwise
    // count as a landing.
    private const int JumpGraceSteps = 3;
    // The ground probe starts this far above the feet, so it still finds ground
    // the capsule has sunk into a little.
    private const float ProbeLift = 0.2f;
    // Shorter flights (a stair, a kerb) are not landings.
    private const float MinLandingAirTime = 0.2f;
    // The capsule reports surfaces this close, so the solver stops a fall at the
    // ground instead of letting the body sink in for a step and popping it back:
    // 0.2 m covers a landing at 10 m/s. Only contacts closer than TouchDistance
    // count as standing on something.
    private const float ContactOffset = 0.2f;
    private const float TouchDistance = 0.02f;
    // A ladder: looking further down than this, forward climbs down.
    private const float LadderLookDown = -35f;
    // The feet go this far above the top before stepping off, to clear the deck's edge.
    private const float LadderStepOffLift = 0.1f;
    // Ground this far below the top is the bottom of a ladder: forward climbs from it.
    // Higher ground is the deck the ladder leads to, and the keys walk on it.
    private const float LadderDeckMargin = 0.5f;
    // Seconds after letting go of a ladder during which it does not catch the body.
    private const float LadderLetGoTime = 0.4f;

    [Header("Speed, m/s")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;

    [Header("Response, m/s per second")]
    [Tooltip("Gaining speed on the ground: walk speed is reached in walkSpeed / acceleration seconds")]
    [SerializeField] private float acceleration = 45f;
    [Tooltip("Stopping on the ground once the keys are released; also used to reverse")]
    [SerializeField] private float deceleration = 60f;
    [Tooltip("Steering in the air. With no keys held the body keeps its momentum")]
    [SerializeField] private float airAcceleration = 15f;

    [Header("Jump")]
    [Tooltip("Height of the jump, m")]
    [SerializeField] private float jumpHeight = 0.9f;
    [Tooltip("Gravity on the way up, in g. Higher = shorter, snappier takeoff")]
    [SerializeField] private float riseGravity = 2.2f;
    [Tooltip("Gravity on the way down, in g. Higher = heavier landing")]
    [SerializeField] private float fallGravity = 3f;
    [SerializeField] private float maxFallSpeed = 30f;
    [Tooltip("Seconds after walking off an edge during which a jump still works")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("Seconds a jump pressed just before landing is remembered")]
    [SerializeField] private float jumpBuffer = 0.12f;

    [Header("Ground")]
    [Tooltip("Steeper surfaces are not ground: the body slides off them")]
    [SerializeField, Range(0f, 89f)] private float maxSlopeAngle = 50f;
    [Tooltip("Ledges up to this height are walked over without a jump, m")]
    [SerializeField] private float stepHeight = 0.3f;
    [Tooltip("How far below the feet the ground may be for the body to stay on it over a crest or down a step, m")]
    [SerializeField] private float groundProbeDistance = 0.45f;
    [Tooltip("How fast the body is pulled down to the ground it stays on, m/s")]
    [SerializeField] private float stickSpeed = 5f;
    [Tooltip("Faster than this the body is not held to the ground: it was thrown, not walking")]
    [SerializeField] private float maxStickSpeed = 14f;

    [Header("Ladder")]
    [Tooltip("Up and down the rails, and along them sideways, m/s")]
    [SerializeField] private float climbSpeed = 2.5f;

    [Inject] private InputService input;

    private readonly List<Func<float>> speedLimits = new List<Func<float>>();
    private Rigidbody body;
    private PlayerLook look;

    private float minGroundDot;
    private float minStepDot;

    // Contacts gathered since the last FixedUpdate.
    private Vector3 groundNormal;
    private int groundContacts;
    private Vector3 stepNormal;
    private int stepContacts;

    private int stepsSinceJump = JumpGraceSteps + 1;
    private float coyoteLeft;
    private float jumpBufferLeft;
    private float airTime;
    private float ladderLetGoLeft;

    public event Action Jumped;
    public event Action Landed;

    public bool IsGrounded { get; private set; }
    // Set by Swimming. Water carries the body: gravity is plain g, which Buoyancy
    // balances against, and the legs steer as well as on the ground.
    public bool IsInWater { get; set; }
    // Set by a Ladder while the body is at it: the keys climb instead of walking.
    public Ladder Ladder { get; set; }
    public float PlanarSpeed { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        look = GetComponentInChildren<PlayerLook>();

        // Gravity is applied in FixedUpdate. A sleeping body gets no contact
        // callbacks and would lose the ground under its feet.
        body.useGravity = false;
        body.sleepThreshold = 0f;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        capsule.contactOffset = ContactOffset;

        float radius = capsule.radius;
        minGroundDot = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        // The capsule's round bottom touches a ledge of height h with a normal whose
        // y is 1 - h / radius.
        minStepDot = Mathf.Min(minGroundDot, 1f - stepHeight / radius);
    }

    private void OnEnable()
    {
        input.OnJump += RequestJump;
    }

    private void OnDisable()
    {
        input.OnJump -= RequestJump;
    }

    // The lowest limit wins; walking and running never exceed it.
    public void AddSpeedLimit(Func<float> limit) => speedLimits.Add(limit);
    public void RemoveSpeedLimit(Func<float> limit) => speedLimits.Remove(limit);

    private void RequestJump()
    {
        jumpBufferLeft = jumpBuffer;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 velocity = body.linearVelocity;
        stepsSinceJump++;

        bool wasGrounded = IsGrounded;
        IsGrounded = FindGround(ref velocity, dt, out Vector3 normal);
        if (IsGrounded)
        {
            coyoteLeft = coyoteTime;
            if (!wasGrounded && airTime >= MinLandingAirTime)
                Landed?.Invoke();
            airTime = 0f;
        }
        else
        {
            coyoteLeft -= dt;
            airTime = IsInWater ? 0f : airTime + dt;
        }

        ladderLetGoLeft -= dt;
        if (Ladder != null && ladderLetGoLeft <= 0f && Climb(ref velocity))
        {
            // Hanging on a ladder is not a fall: letting go starts the air time anew.
            airTime = 0f;
            EndStep(velocity, 0f);
            return;
        }

        Steer(ref velocity, normal, dt);
        ApplyGravity(ref velocity, normal, dt);

        jumpBufferLeft -= dt;
        if (jumpBufferLeft > 0f && coyoteLeft > 0f)
            Jump(ref velocity, dt);

        EndStep(velocity, new Vector2(velocity.x, velocity.z).magnitude);
    }

    private void EndStep(Vector3 velocity, float planarSpeed)
    {
        body.linearVelocity = velocity;
        // The camera turns every frame on its own (PlayerLook); the body follows at
        // the physics rate. Turning it through the Transform would break interpolation.
        body.MoveRotation(Quaternion.Euler(0f, look.Yaw, 0f));
        PlanarSpeed = planarSpeed;

        groundNormal = stepNormal = Vector3.zero;
        groundContacts = stepContacts = 0;
    }

    // On a ladder the keys climb: forward goes up the rails (down, if looking down),
    // back goes down, sideways moves along it; with no keys the body hangs. At the
    // top forward steps off onto the deck the ladder leans on. Jump lets go.
    // False when the legs walk instead: on the ground at its foot unless forward is
    // held, and anywhere on the deck at its top.
    private bool Climb(ref Vector3 velocity)
    {
        Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
        float upward = look.Pitch < LadderLookDown ? -move.y : move.y;
        float feet = body.position.y;
        if (IsGrounded && (move.y <= 0f || upward <= 0f || feet > Ladder.TopY - LadderDeckMargin))
            return false;

        if (jumpBufferLeft > 0f)
        {
            jumpBufferLeft = 0f;
            ladderLetGoLeft = LadderLetGoTime;
            velocity = (Vector3.up - Ladder.Toward) * climbSpeed;
            return true;
        }

        if (upward > 0f && feet >= Ladder.TopY + LadderStepOffLift)
        {
            velocity = Ladder.Toward * climbSpeed;
            return true;
        }

        Vector3 side = Vector3.Cross(Vector3.up, Ladder.Toward);
        velocity = Ladder.Up * (upward * climbSpeed) + side * (move.x * climbSpeed);
        return true;
    }

    private void OnCollisionEnter(Collision collision) => ReadContacts(collision);
    private void OnCollisionStay(Collision collision) => ReadContacts(collision);

    private void ReadContacts(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.separation > TouchDistance)
                continue;

            Vector3 normal = contact.normal;
            if (normal.y >= minGroundDot)
            {
                groundNormal += normal;
                groundContacts++;
            }
            else if (normal.y >= minStepDot)
            {
                stepNormal += normal;
                stepContacts++;
            }
        }
    }

    // True when the body stands on something, with the plane to move in.
    private bool FindGround(ref Vector3 velocity, float dt, out Vector3 normal)
    {
        normal = Vector3.up;
        if (stepsSinceJump <= JumpGraceSteps)
            return false;

        // A ledge touched while standing tilts the plane toward its edge, so the
        // body climbs it at full speed instead of butting into it.
        if (groundContacts > 0)
        {
            normal = (groundNormal + stepNormal).normalized;
            return true;
        }

        // Nothing walkable touches the capsule. Any layer but Ignore Raycast, where
        // the water volumes are; triggers are not ground.
        Vector3 origin = body.position + Vector3.up * ProbeLift;
        bool isGroundBelow = Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            ProbeLift + groundProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (!isGroundBelow || hit.normal.y < minGroundDot)
            return false;

        // Leaning on the edge of a ledge with the floor right below: mid-step.
        if (stepContacts > 0)
        {
            normal = stepNormal.normalized;
            return true;
        }

        // The ground fell away this very step — a crest, the top of a ramp, a stair
        // down. Turn the velocity along the surface below and pull the body onto it,
        // otherwise every bump is a ramp. IsGrounded still holds the previous step.
        float speed = velocity.magnitude;
        if (!IsGrounded || speed > maxStickSpeed)
            return false;

        normal = hit.normal;
        float away = Vector3.Dot(velocity, normal);
        if (away > 0f)
            velocity = (velocity - normal * away).normalized * speed;

        float closing = Mathf.Clamp((hit.distance - ProbeLift) / dt, 0f, stickSpeed);
        float toward = -Vector3.Dot(velocity, normal);
        if (toward < closing)
            velocity -= normal * (closing - toward);
        return true;
    }

    private void Steer(ref Vector3 velocity, Vector3 normal, float dt)
    {
        Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
        bool hasTraction = IsGrounded || IsInWater;
        Vector3 planar = Vector3.ProjectOnPlane(velocity, normal);

        Vector3 target = Vector3.zero;
        float rate = deceleration;
        if (move != Vector2.zero)
        {
            // Keep the heading and tilt the direction into the plane, so the speed is
            // the speed along the slope.
            Vector3 wish = Quaternion.Euler(0f, look.Yaw, 0f) * new Vector3(move.x, 0f, move.y);
            wish.y = -(wish.x * normal.x + wish.z * normal.z) / normal.y;
            target = wish.normalized * (TopSpeed() * move.magnitude);

            if (!hasTraction)
                rate = airAcceleration;
            else if (Vector3.Dot(planar, target) < 0f)
                rate = Mathf.Max(acceleration, deceleration);
            else
                rate = acceleration;
        }
        else if (!hasTraction)
        {
            return;
        }

        velocity += Vector3.MoveTowards(planar, target, rate * dt) - planar;
    }

    private float TopSpeed()
    {
        float speed = input.IsRunHeld ? runSpeed : walkSpeed;
        for (int i = 0; i < speedLimits.Count; i++)
            speed = Mathf.Min(speed, speedLimits[i]());
        return speed;
    }

    private void ApplyGravity(ref Vector3 velocity, Vector3 normal, float dt)
    {
        // On the ground only the part pressing into it: the part along the slope
        // would make the body creep downhill while standing still.
        if (IsGrounded)
        {
            velocity += normal * (Vector3.Dot(Physics.gravity, normal) * dt);
            return;
        }

        if (IsInWater)
        {
            velocity += Physics.gravity * dt;
            return;
        }

        float scale = velocity.y > 0f ? riseGravity : fallGravity;
        velocity += Physics.gravity * (scale * dt);
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    private void Jump(ref Vector3 velocity, float dt)
    {
        // Takeoff speed that reaches jumpHeight under riseGravity. The first step
        // is flown before gravity is applied, hence the half step taken off.
        float gravity = -Physics.gravity.y * riseGravity;
        float jumpSpeed = Mathf.Sqrt(2f * gravity * jumpHeight) - 0.5f * gravity * dt;
        velocity.y = Mathf.Max(velocity.y, jumpSpeed);

        jumpBufferLeft = 0f;
        coyoteLeft = 0f;
        stepsSinceJump = 0;
        IsGrounded = false;
        Jumped?.Invoke();
    }
}
