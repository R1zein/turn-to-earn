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
        float maxDistance = float.PositiveInfinity)
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

                float score = weight.Weight / distance;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = body.transform;
                }
            }
        }
        return best;
    }

    public T FindNearest<T>(TargetKind kind, Vector3 from, float maxDistance) where T : Component
    {
        T nearest = null;
        float nearestDistance = maxDistance;
        foreach (Component body in registry.Get(kind))
        {
            float distance = Vector3.Distance(from, body.transform.position);
            if (distance <= nearestDistance && body is T typed)
            {
                nearestDistance = distance;
                nearest = typed;
            }
        }
        return nearest;
    }
}
