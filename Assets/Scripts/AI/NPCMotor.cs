using UnityEngine;
using UnityEngine.AI;

// The agent plans and avoids neighbours. Physics owns the body's movement.
[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody), typeof(CapsuleCollider))]
public class NPCMotor : MonoBehaviour
{
    private const float RepathInterval = 0.35f;
    private const float DestinationShift = 0.5f;
    private const float ReattachDistance = 2f;
    private const float GroundLift = 0.2f;
    private const float GroundContact = 0.08f;
    private const float GroundFollow = 0.32f;
    private const float MaxStickSpeed = 5f;
    private const float PushGrace = 0.12f;

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private NavMeshAgent agent;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private Vector3 orderedPoint;
    private float nextRepath;
    private float pushUntil;
    private bool hasOrder;
    private Vector3? facingPoint;

    public bool IsGrounded { get; private set; }
    public bool IsAirborne { get; private set; }
    public Vector3 Velocity => body.linearVelocity;
    public bool IsMoving => Horizontal(Velocity).sqrMagnitude > 0.04f;
    public bool IsTraversingLink => IsOnNavMesh && agent.isOnOffMeshLink;
    private bool IsOnNavMesh => agent.enabled && agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.autoTraverseOffMeshLink = false;
    }

    private void OnEnable() => agent.enabled = true;

    private void OnDisable()
    {
        agent.enabled = false;
        hasOrder = false;
    }

    public bool TrySample(Vector3 point, float radius, out NavMeshHit hit)
    {
        // The spawner also samples using the agent settings on a prefab asset.
        NavMeshAgent settings = agent != null ? agent : GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter { agentTypeID = settings.agentTypeID, areaMask = settings.areaMask };
        return NavMesh.SamplePosition(point, out hit, radius, filter);
    }

    public bool HasRoute(Vector3 point, NavMeshPath path) => IsOnNavMesh && !IsAirborne
        && agent.CalculatePath(point, path) && path.status == NavMeshPathStatus.PathComplete;

    public bool HasOrder => hasOrder && IsOnNavMesh && (agent.pathPending || agent.hasPath);

    public void MoveTo(Vector3 point)
    {
        facingPoint = null;
        if (!IsOnNavMesh || IsAirborne || IsTraversingLink)
            return;

        agent.isStopped = false;
        if (hasOrder && Time.time < nextRepath && (point - orderedPoint).sqrMagnitude < DestinationShift * DestinationShift)
            return;

        hasOrder = agent.SetDestination(point);
        orderedPoint = point;
        nextRepath = Time.time + RepathInterval;
    }

    public void Stop()
    {
        facingPoint = null;
        if (IsOnNavMesh && !IsTraversingLink)
            agent.isStopped = true;
    }

    public void Face(Vector3 point)
    {
        facingPoint = point;
    }

    public void Push(Vector3 impulse)
    {
        Stop();
        IsAirborne = true;
        IsGrounded = false;
        pushUntil = Time.time + PushGrace;
        body.AddForce(impulse, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        RaycastHit ground = default;
        float gap = 0f;
        IsGrounded = (!IsAirborne || (Time.time >= pushUntil && velocity.y <= 0.1f))
            && FindGround(out ground, out gap);
        if (IsGrounded && (IsAirborne || !IsOnNavMesh))
            Reattach();
        if (IsAirborne)
            return;

        if (!IsTraversingLink && IsOnNavMesh)
            agent.nextPosition = body.position;

        if (!IsGrounded)
            return;

        Vector3 wish = DesiredVelocity();
        float dt = Time.fixedDeltaTime;
        Vector3 planar = Vector3.MoveTowards(Horizontal(velocity), wish, agent.acceleration * dt);
        Vector3 normal = ground.normal;
        float vertical = -Vector3.Dot(planar, normal) / normal.y;
        // Follow the down-slope or a short step when leaving a crest.
        vertical -= Mathf.Clamp(gap / dt, 0f, MaxStickSpeed);
        body.linearVelocity = planar + Vector3.up * vertical;
        Turn(facingPoint is Vector3 point && !IsTraversingLink ? Horizontal(point - body.position) : planar);
    }

    private bool FindGround(out RaycastHit support, out float gap)
    {
        Bounds bounds = capsule.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + GroundLift, bounds.center.z);
        float reach = IsGrounded ? GroundFollow : GroundContact;
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
            GroundLift + reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        support = default;
        float closest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.rigidbody == body || (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                || hit.normal.y < 0.38f || hit.distance >= closest)
                continue;
            closest = hit.distance;
            support = hit;
        }
        gap = closest - GroundLift;
        return closest < float.PositiveInfinity;
    }

    private void Reattach()
    {
        IsAirborne = false;
        if (IsOnNavMesh && (agent.nextPosition - body.position).sqrMagnitude < DestinationShift * DestinationShift)
            return;

        if (TrySample(body.position, ReattachDistance, out NavMeshHit hit))
        {
            Vector3 physicalPosition = body.position;
            agent.Warp(hit.position);
            body.position = physicalPosition;
        }
        hasOrder = false;
    }

    private Vector3 DesiredVelocity()
    {
        if (!IsOnNavMesh)
            return Vector3.zero;
        if (!IsTraversingLink)
            return agent.isStopped ? Vector3.zero : Horizontal(agent.desiredVelocity);

        // The mountain route already has a directed link. Let the body walk
        // along its slope and hand control back to the agent at the landing.
        Vector3 end = agent.currentOffMeshLinkData.endPos + Vector3.up * agent.baseOffset;
        Vector3 offset = end - body.position;
        Vector3 direction = Horizontal(offset);
        if (direction.sqrMagnitude <= 0.25f && Mathf.Abs(offset.y) < 1.1f)
        {
            agent.CompleteOffMeshLink();
            return Vector3.zero;
        }
        return Vector3.ClampMagnitude(direction / Time.fixedDeltaTime, agent.speed);
    }

    private void Turn(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction),
                agent.angularSpeed * Time.fixedDeltaTime));
    }

    private Vector3 Horizontal(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
}
