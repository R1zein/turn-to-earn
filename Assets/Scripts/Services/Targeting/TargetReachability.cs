using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// A ranked candidate is probed only when it could actually be selected.
internal class TargetReachability
{
    private const float RecheckInterval = 2.5f;
    private const float SampleRadius = 1.15f;
    private const float MaxHeightDifference = 0.8f;
    private const float HeightChange = 0.45f;
    private const float FailedTargetShift = 2f;
    private const float SeekerShift = 10f;
    private const float DestinationShift = 0.5f;

    private class Route
    {
        public float Expires;
        public Vector3 CheckedFrom;
        public Vector3 CheckedAnchor;
        public Vector3 SampledAnchor;
        public Vector3 Destination;
        public bool IsReachable;
    }

    private readonly Dictionary<TargetSeeker, Dictionary<Targetable, Route>> routes =
        new Dictionary<TargetSeeker, Dictionary<Targetable, Route>>();
    private readonly NavMeshPath path = new NavMeshPath();

    public bool TryGetDestination(TargetSeeker seeker, Targetable target, out Vector3 destination)
    {
        NPCMotor motor = seeker.Motor;
        Vector3 origin = seeker.Origin;
        Vector3 anchor = target.NavigationAnchor(origin);
        Route route = GetRoute(seeker, target);
        float now = Time.time;

        if (now >= route.Expires || Mathf.Abs(anchor.y - route.CheckedAnchor.y) >= HeightChange
            || (origin - route.CheckedFrom).sqrMagnitude >= SeekerShift * SeekerShift
            || (!route.IsReachable && (anchor - route.CheckedAnchor).sqrMagnitude >= FailedTargetShift * FailedTargetShift))
        {
            route.Expires = now + RecheckInterval;
            route.CheckedFrom = origin;
            route.CheckedAnchor = anchor;
            route.SampledAnchor = anchor;
            route.IsReachable = TrySample(motor, anchor, out route.Destination) && motor.HasRoute(route.Destination, path);
        }
        else if (route.IsReachable && (anchor - route.SampledAnchor).sqrMagnitude >= DestinationShift * DestinationShift)
        {
            // Update the pursuit point for a moving target without recalculating
            // the whole path. The motor reports a partial path between checks.
            route.SampledAnchor = anchor;
            route.IsReachable = TrySample(motor, anchor, out route.Destination);
        }

        destination = route.Destination;
        return route.IsReachable;
    }

    private Route GetRoute(TargetSeeker seeker, Targetable target)
    {
        if (!routes.TryGetValue(seeker, out var byTarget))
        {
            byTarget = new Dictionary<Targetable, Route>();
            routes.Add(seeker, byTarget);
        }
        if (!byTarget.TryGetValue(target, out Route route))
        {
            route = new Route();
            byTarget.Add(target, route);
        }
        return route;
    }

    private bool TrySample(NPCMotor motor, Vector3 anchor, out Vector3 destination)
    {
        destination = default;
        if (!motor.TrySample(anchor, SampleRadius, out NavMeshHit hit)
            || Mathf.Abs(hit.position.y - anchor.y) > MaxHeightDifference)
            return false;

        destination = hit.position;
        return true;
    }

    public void Invalidate(TargetSeeker seeker, Targetable target)
    {
        if (routes.TryGetValue(seeker, out var byTarget))
            byTarget.Remove(target);
    }

    public void Forget(TargetSeeker seeker) => routes.Remove(seeker);

    public void Forget(Targetable target)
    {
        foreach (var byTarget in routes.Values)
            byTarget.Remove(target);
    }
}
