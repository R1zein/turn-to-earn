using UnityEngine;

// Shared line-of-sight and swept bolt queries, independent of target assignment.
public class WeaponSight
{
    public bool CanSee(Transform owner, Vector3 origin, Targetable target)
    {
        if (target == null || !target.IsAvailable || target.Health == null)
            return false;

        Vector3 direction = target.AimPoint - origin;
        return Trace(owner, new Ray(origin, direction.normalized), direction.magnitude) is not RaycastHit hit
            || hit.transform.IsChildOf(target.transform);
    }

    public RaycastHit? Trace(Transform owner, Ray ray, float distance)
    {
        if (!Physics.Raycast(ray, out RaycastHit first, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return null;
        if (owner == null || !first.transform.IsChildOf(owner))
            return first;

        RaycastHit? closest = null;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(owner) && (!closest.HasValue || hit.distance < closest.Value.distance))
                closest = hit;
        }
        return closest;
    }
}
