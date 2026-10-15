using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Each time the wave period begins, every portal opens and releases enemies one
// by one: base count + 2 per wave so far. From wave SpiderFirstWave on, spiders
// follow the zombies - SpidersPerWave more each wave, dealt out one per portal
// starting from a different portal every wave. Waves still running when the
// scene unloads stop at their next step.
public class WaveService : IInitializable, IDisposable
{
    [Inject] private DayCycle dayCycle;
    [Inject] private GameConfig config;
    [Inject] private LevelAnchors anchors;
    [Inject] private EnemySpawner spawner;

    // A portal still spawning the previous wave does not start a second one on top.
    private readonly HashSet<Portal> busyPortals = new HashSet<Portal>();
    private bool isDisposed;

    public void Initialize()
    {
        dayCycle.OnPeriodEnter += OnPeriodEnter;
    }

    public void Dispose()
    {
        dayCycle.OnPeriodEnter -= OnPeriodEnter;
        isDisposed = true;
    }

    private void OnPeriodEnter(TimePeriod period)
    {
        if (period != config.WavePeriod)
            return;

        int wave = dayCycle.EnterCount(period);
        int zombies = config.WaveBaseCount + wave * 2;
        IReadOnlyList<Portal> portals = anchors.Portals;
        int[] spiders = DealSpiders(wave, portals.Count);
        for (int i = 0; i < portals.Count; i++)
            _ = RunWave(portals[i], zombies, spiders[i]);
    }

    // Spiders per portal for this wave.
    private int[] DealSpiders(int wave, int portalCount)
    {
        var perPortal = new int[portalCount];
        int total = SpiderCount(wave);
        for (int k = 0; k < total; k++)
            perPortal[(wave + k) % portalCount]++;
        return perPortal;
    }

    private int SpiderCount(int wave)
    {
        if (config.SpiderEnemy == null || wave < config.SpiderFirstWave)
            return 0;
        return (wave - config.SpiderFirstWave + 1) * config.SpidersPerWave;
    }

    private async Awaitable RunWave(Portal portal, int zombies, int spiders)
    {
        if (!busyPortals.Add(portal))
            return;

        portal.SetOpen(true);
        for (int i = 0; i < zombies + spiders; i++)
        {
            Enemy prefab = i < zombies ? config.WaveEnemy : config.SpiderEnemy;
            spawner.Spawn(prefab, portal.SpawnPosition);
            await Awaitable.WaitForSecondsAsync(config.WaveSpawnInterval);
            // The scene unloaded while we waited: the portal is gone with it.
            if (isDisposed)
                return;
        }
        portal.SetOpen(false);
        busyPortals.Remove(portal);
    }
}
