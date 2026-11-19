using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

[Serializable]
public struct TargetWeight
{
    public TargetKind Kind;
    public float Weight;
}

// Decides where NPCs and turrets go. One pass over the registry, every kind at
// once: score = weight / distance, the highest score wins.
public class TargetingService
{
    [Inject] private TargetRegistry registry;

    public Transform FindBest(Vector3 from, IReadOnlyList<TargetWeight> weights,
        float maxDistance = float.PositiveInfinity, Predicate<Component> isAvailable = null)
    {
        Transform best = null;
        float bestScore = 0f;
        foreach (TargetWeight weight in weights)
        {
            foreach (Component body in registry.Get(weight.Kind))
            {
                float distance = Vector3.Distance(from, body.transform.position);
                if (distance > maxDistance)
                    continue;

                float score = weight.Weight / Mathf.Max(1f, distance);
                if (score > bestScore && (isAvailable == null || isAvailable(body)))
                {
                    bestScore = score;
                    best = body.transform;
                }
            }
        }
        return best;
    }

    public T FindNearest<T>(TargetKind kind, Vector3 from, float maxDistance,
        Predicate<T> isAvailable = null) where T : Component
    {
        T nearest = null;
        float nearestDistance = maxDistance;
        foreach (Component body in registry.Get(kind))
        {
            float distance = Vector3.Distance(from, body.transform.position);
            if (distance <= nearestDistance && body is T typed && (isAvailable == null || isAvailable(typed)))
            {
                nearestDistance = distance;
                nearest = typed;
            }
        }
        return nearest;
    }

    public NPCFacade FindVisibleEnemy(Transform owner, Vector3 origin, float range) =>
        FindNearest<NPCFacade>(TargetKind.Enemy, origin, range,
            enemy => CanSeeEnemy(owner, origin, enemy, range));

    public bool CanSeeEnemy(Transform owner, Vector3 origin, NPCFacade enemy, float range)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.Health.IsDead)
            return false;

        Vector3 direction = enemy.AimPoint - origin;
        float distance = direction.magnitude;
        return distance <= range && (Trace(owner, new Ray(origin, direction.normalized), distance) is not RaycastHit hit
            || hit.transform.IsChildOf(enemy.transform));
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
