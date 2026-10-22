using System;
using UnityEngine;
using Zenject;

// Keeps every spot of the shooting range manned: a dummy that stands still
// (Enemy.HoldStill) and, once killed, is replaced after GameConfig.RangeRespawnDelay.
// Respawns still pending when the scene unloads are dropped at their next step.
public class ShootingRangeService : IInitializable, IDisposable
{
    [Inject] private ShootingRange range;
    [Inject] private EnemySpawner spawner;
    [Inject] private GameConfig config;

    private bool isDisposed;

    public void Initialize()
    {
        foreach (Transform spot in range.Spots)
            Man(spot);
    }

    public void Dispose()
    {
        isDisposed = true;
    }

    private void Man(Transform spot)
    {
        Enemy dummy = spawner.Spawn(config.RangeTarget, spot.position, spot.rotation);
        dummy.HoldStill();
        dummy.GetComponent<StatsHandler>().OnDeath += () => _ = Respawn(spot);
    }

    private async Awaitable Respawn(Transform spot)
    {
        await Awaitable.WaitForSecondsAsync(config.RangeRespawnDelay);
        // The scene unloaded while we waited: the range is gone with it.
        if (isDisposed)
            return;
        Man(spot);
    }
}
