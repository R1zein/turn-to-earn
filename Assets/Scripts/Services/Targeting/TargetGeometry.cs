using System.Collections.Generic;
using UnityEngine;

// Target shapes can include separate solid parts, such as the leaves of a gate.
public class TargetGeometry
{
    private readonly Transform root;
    private readonly List<Collider> solids = new List<Collider>();

    public TargetGeometry(Transform root)
    {
        this.root = root;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger)
                solids.Add(collider);
    }

    public Vector3 AimPoint
    {
        get
        {
            foreach (Collider solid in solids)
                if (IsActive(solid))
                    return solid.bounds.center;
            return root.position;
        }
    }

    public Vector3 ClosestPoint(Vector3 from)
    {
        Vector3 nearest = root.position;
        float best = float.PositiveInfinity;
        foreach (Collider solid in solids)
        {
            if (!IsActive(solid))
                continue;

            Bounds bounds = solid.bounds;
            if (bounds.SqrDistance(from) >= best)
                continue;

            Vector3 point = SurfacePoint(solid, bounds, from);
            float distance = (point - from).sqrMagnitude;
            if (distance < best)
            {
                nearest = point;
                best = distance;
            }
        }
        return nearest;
    }

    private bool IsActive(Collider solid) => solid != null && solid.enabled && solid.gameObject.activeInHierarchy;

    private Vector3 SurfacePoint(Collider solid, Bounds bounds, Vector3 from)
    {
        if (solid is BoxCollider or SphereCollider or CapsuleCollider || solid is MeshCollider { convex: true })
            return solid.ClosestPoint(from);

        // Unity's ClosestPoint does not support concave meshes. Where possible,
        // use the real face hit by a ray instead of treating the whole box as solid.
        Vector3 boundPoint = bounds.ClosestPoint(from);
        Vector3 edge = boundPoint - from;
        if (edge.sqrMagnitude > 0.0001f && solid.Raycast(new Ray(from, edge.normalized), out RaycastHit nearHit,
            edge.magnitude + bounds.extents.magnitude))
            return nearHit.point;

        Vector3 delta = bounds.center - from;
        if (delta.sqrMagnitude > 0.0001f && solid.Raycast(new Ray(from, delta.normalized), out RaycastHit hit, delta.magnitude))
            return hit.point;
        return boundPoint;
    }
}
