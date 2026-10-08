using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Zenject;

// Each time the wave period begins, every portal opens and releases enemies one
// by one: base count + 2 per wave so far. Waves still running when the scene
// unloads are cancelled in Dispose.
public class WaveService : IInitializable, IDisposable
{
    [Inject] private DayCycle dayCycle;
    [Inject] private GameConfig config;
    [Inject] private LevelAnchors anchors;
    [Inject] private EnemySpawner spawner;

    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    // A portal still spawning the previous wave does not start a second one on top.
    private readonly HashSet<Portal> busyPortals = new HashSet<Portal>();

    public void Initialize()
    {
        dayCycle.OnPeriodEnter += OnPeriodEnter;
    }

    public void Dispose()
    {
        dayCycle.OnPeriodEnter -= OnPeriodEnter;
        lifetime.Cancel();
        lifetime.Dispose();
    }

    private void OnPeriodEnter(TimePeriod period)
    {
        if (period != config.WavePeriod)
            return;

        int count = config.WaveBaseCount + dayCycle.EnterCount(period) * 2;
        foreach (Portal portal in anchors.Portals)
            _ = RunWave(portal, count);
    }

    private async Awaitable RunWave(Portal portal, int count)
    {
        if (!busyPortals.Add(portal))
            return;

        portal.SetOpen(true);
        try
        {
            for (int i = 0; i < count; i++)
            {
                spawner.Spawn(config.WaveEnemy, portal.SpawnPosition);
                await Awaitable.WaitForSecondsAsync(config.WaveSpawnInterval, lifetime.Token);
            }
            portal.SetOpen(false);
        }
        catch (OperationCanceledException)
        {
            // Scene is unloading; the portal goes with it.
        }
        finally
        {
            busyPortals.Remove(portal);
        }
    }
}
