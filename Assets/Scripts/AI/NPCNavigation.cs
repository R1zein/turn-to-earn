using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

// Body side of choosing a target: holds the current target and walks the agent
// to it. Which target is best is TargetingService's decision.
public class NPCNavigation : MonoBehaviour
{
    private const float RescanInterval = 0.6f;
    private const float PlayerNavMeshTolerance = 0.85f;
    private const float NavMeshSearch = 2f;

    [Inject] private TargetingService targeting;
    [InjectOptional] private Terrain terrain;

    public float sightDistance;
    public float attackDistance;
    private NavMeshAgent agent;
    private NavMeshPath path;
    private Vector3 destination;
    private Vector3 orderedDestination;
    private Vector3? approachPoint;
    private bool hasOrder;
    private bool isTraversing;
    private float linkTravel;
    private Vector3 linkStart;
    private float timer;

    public Transform target { get; private set; }
    public bool IsMoving => isTraversing || (agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.04f);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        path = new NavMeshPath();
        // Stagger path queries for agents released together.
        timer = Random.Range(0f, RescanInterval);
    }

    public bool InAttackRange(float range)
    {
        if (target == null || isTraversing || (agent.isOnNavMesh && agent.isOnOffMeshLink))
            return false;

        Vector3 point = ContactPoint(target);
        Vector3 delta = point - transform.position;
        if (delta.sqrMagnitude > range * range)
            return false;

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 aim = delta + transform.position - origin + Vector3.up * 0.5f;
        return !Physics.Raycast(origin, aim.normalized, out RaycastHit hit, aim.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(target);
    }

    // Whole map, once when the NPC appears: find somewhere to go at all.
    public void LookEverywhere(IReadOnlyList<TargetWeight> weights)
    {
        if (!FollowApproach())
            Select(weights, float.PositiveInfinity);
    }

    public void Approach(Vector3 waypoint) => approachPoint = waypoint;

    public void Rescan(IReadOnlyList<TargetWeight> weights)
    {
        if (agent.enabled && agent.isOnNavMesh && agent.isOnOffMeshLink)
        {
            TraverseLink();
            return;
        }
        isTraversing = false;
        timer += Time.deltaTime;
        if (timer < RescanInterval)
            return;
        timer = 0f;

        if (!FollowApproach() && !Select(weights, sightDistance))
            Select(weights, float.PositiveInfinity);
    }

    private bool FollowApproach()
    {
        if (approachPoint is not Vector3 waypoint)
            return false;

        if ((waypoint - transform.position).sqrMagnitude > 1f && agent.enabled && agent.isOnNavMesh)
        {
            if (hasOrder && (agent.pathPending || (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)))
                return true;

            // The mountain portal is far enough away that one path query to the
            // player can come back partial. Route through the ridge landing first.
            if (agent.CalculatePath(waypoint, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.stoppingDistance = 0.15f;
                hasOrder = agent.SetPath(path);
                if (hasOrder)
                    return true;
            }
        }

        approachPoint = null;
        hasOrder = false;
        return false;
    }

    private bool Select(IReadOnlyList<TargetWeight> weights, float range)
    {
        if (!agent.enabled || (!agent.isOnNavMesh && !ConnectToNavMesh()))
        {
            target = null;
            hasOrder = false;
            return false;
        }

        Transform best = targeting.FindBest(transform.position, weights, range, HasRoute);
        if (best == null)
        {
            target = null;
            if (hasOrder)
                agent.ResetPath();
            hasOrder = false;
            return false;
        }

        if (!hasOrder || target != best || (destination - orderedDestination).sqrMagnitude > 0.09f
            || (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            agent.stoppingDistance = best.TryGetComponent<Structure>(out _) ? 0.15f : 0.65f;
            hasOrder = agent.SetDestination(destination);
            orderedDestination = destination;
        }
        target = best;
        return true;
    }

    private bool HasRoute(Component candidate)
    {
        Transform body = candidate.transform;
        if (body == transform || !body.gameObject.activeInHierarchy
            || (body.TryGetComponent(out StatsHandler stats) && stats.IsDead))
            return false;

        Vector3 point = ContactPoint(body);
        bool isPlayer = candidate is Player;
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, isPlayer ? PlayerNavMeshTolerance : NavMeshSearch, AgentFilter())
            || !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        destination = hit.position;
        return true;
    }

    private bool ConnectToNavMesh() =>
        NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSearch, AgentFilter()) && agent.Warp(hit.position);

    private NavMeshQueryFilter AgentFilter() =>
        new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };

    private void TraverseLink()
    {
        if (!isTraversing)
        {
            isTraversing = true;
            linkTravel = 0f;
            linkStart = transform.position;
        }

        Vector3 end = agent.currentOffMeshLinkData.endPos + Vector3.up * agent.baseOffset;
        linkTravel += agent.speed * Time.deltaTime;
        float length = Vector3.Distance(linkStart, end);
        float progress = length < 0.01f ? 1f : Mathf.Clamp01(linkTravel / length);
        Vector3 next = Vector3.Lerp(linkStart, end, progress);
        if (terrain != null && progress < 1f)
        {
            // The ridge is too steep to bake but solid all the way down. Follow
            // its height so the agent does not disappear through the terrain.
            float ground = terrain.SampleHeight(next) + terrain.transform.position.y + agent.baseOffset;
            if (Mathf.Abs(ground - next.y) < 6f)
                next.y = ground;
        }

        Vector3 horizontal = Vector3.ProjectOnPlane(end - linkStart, Vector3.up);
        if (horizontal.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(horizontal), agent.angularSpeed * Time.deltaTime);
        transform.position = next;
        if (progress >= 1f)
            agent.CompleteOffMeshLink();
    }

    private Vector3 ContactPoint(Transform body)
    {
        if (!body.TryGetComponent<Structure>(out _) || !body.TryGetComponent(out Collider solid) || solid.isTrigger)
            return body.position;

        // A carved structure is approached at its near edge. Its pivot can be too
        // far inside the blocked area to be an attack destination.
        Vector3 ground = transform.position;
        ground.y = body.position.y;
        return solid.bounds.ClosestPoint(ground);
    }
}
