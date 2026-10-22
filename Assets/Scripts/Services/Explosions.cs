using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Area damage. Every enemy body within GameConfig.BombRadius of the blast takes
// damage falling linearly from BombCenterDamage at the centre to BombEdgeDamage
// at the edge - once, however many colliders it has. Friend or foe is decided by
// fraction, as for every other weapon. The radius is sized so a blast clears a
// crowd of about ten zombies: see Docs/drone.md.
public class Explosions
{
    // The visual lives this long; the Hovl effects do not remove themselves.
    private const float EffectLifetime = 4f;

    [Inject] private GameConfig config;

    private readonly Collider[] hits = new Collider[256];
    private readonly HashSet<StatsHandler> struck = new HashSet<StatsHandler>();

    // Returns how many enemies the blast killed.
    public int Blast(Vector3 center)
    {
        if (config.BombEffect != null)
        {
            GameObject effect = Object.Instantiate(config.BombEffect, center, Quaternion.identity);
            effect.transform.localScale = Vector3.one * config.BombEffectScale;
            Object.Destroy(effect, EffectLifetime);
        }

        float radius = config.BombRadius;
        int count = Physics.OverlapSphereNonAlloc(center, radius, hits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        int killed = 0;
        struck.Clear();
        for (int i = 0; i < count; i++)
        {
            StatsHandler stats = hits[i].GetComponentInParent<StatsHandler>();
            if (stats == null || stats.fraction != Fraction.Enemy || stats.IsDead || !struck.Add(stats))
                continue;

            float distance = Vector3.Distance(center, stats.transform.position);
            float damage = Mathf.Lerp(config.BombCenterDamage, config.BombEdgeDamage, distance / radius);
            stats.TakeDamage(damage);
            if (stats.IsDead)
                killed++;
        }
        return killed;
    }
}
