using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

// Retains the existing target selection while NPCMotor owns movement.
public class NPCNavigation : MonoBehaviour
{
    private const float RescanInterval = 0.6f;
    private const float PlayerNavMeshTolerance = 0.85f;
    private const float NavMeshSearch = 2f;

    [Inject] private TargetingService targeting;

    public float sightDistance;
    public float attackDistance;
    private NPCMotor motor;
    private NavMeshPath path;
    private Vector3 destination;
    private Vector3? approachPoint;
    private float timer;

    public Transform target { get; private set; }

    private void Awake()
    {
        motor = GetComponent<NPCMotor>();
        path = new NavMeshPath();
        timer = Random.Range(0f, RescanInterval);
    }

    public bool InAttackRange(float range)
    {
        if (target == null || motor.IsTraversingLink || motor.IsAirborne)
            return false;

        Vector3 point = ContactPoint(target);
        Vector3 delta = point - transform.position;
        if (delta.sqrMagnitude > range * range)
            return false;

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 aim = point + Vector3.up * 0.5f - origin;
        return !Physics.Raycast(origin, aim.normalized, out RaycastHit hit, aim.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(target);
    }

    public void LookEverywhere(IReadOnlyList<TargetWeight> weights)
    {
        if (!FollowApproach())
            Select(weights, float.PositiveInfinity);
    }

    public void Approach(Vector3 waypoint) => approachPoint = waypoint;

    public void Rescan(IReadOnlyList<TargetWeight> weights)
    {
        if (motor.IsTraversingLink || motor.IsAirborne)
            return;

        timer += Time.deltaTime;
        if (timer < RescanInterval)
            return;
        timer = 0f;

        if (!FollowApproach() && !Select(weights, sightDistance))
            Select(weights, float.PositiveInfinity);
    }

    public void Follow()
    {
        if (approachPoint is Vector3 waypoint)
            motor.MoveTo(waypoint);
        else if (target != null)
            motor.MoveTo(destination);
        else
            motor.Stop();
    }

    private bool FollowApproach()
    {
        if (approachPoint is not Vector3 waypoint)
            return false;

        if ((waypoint - transform.position).sqrMagnitude > 1f && (motor.HasOrder || motor.HasRoute(waypoint, path)))
            return true;

        approachPoint = null;
        return false;
    }

    private bool Select(IReadOnlyList<TargetWeight> weights, float range)
    {
        target = targeting.FindBest(transform.position, weights, range, HasRoute);
        return target != null;
    }

    private bool HasRoute(Component candidate)
    {
        Transform body = candidate.transform;
        if (body == transform || !body.gameObject.activeInHierarchy
            || (body.TryGetComponent(out StatsHandler stats) && stats.IsDead))
            return false;

        Vector3 point = ContactPoint(body);
        float radius = candidate is Player ? PlayerNavMeshTolerance : NavMeshSearch;
        if (!motor.TrySample(point, radius, out NavMeshHit hit) || !motor.HasRoute(hit.position, path))
            return false;

        destination = hit.position;
        return true;
    }

    private Vector3 ContactPoint(Transform body)
    {
        if (!body.TryGetComponent<Structure>(out _) || !body.TryGetComponent(out Collider solid) || solid.isTrigger)
            return body.position;

        Vector3 ground = transform.position;
        ground.y = body.position.y;
        return solid.bounds.ClosestPoint(ground);
    }
}
