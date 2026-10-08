using UnityEngine;
using Zenject;

// Starts the game once every service has finished Initialize. Bound last.
public class GameStarter : IInitializable
{
    [Inject] private TimeManager timeManager;

    public void Initialize() => _ = Begin();

    private async Awaitable Begin()
    {
        await Awaitable.NextFrameAsync();
        timeManager.Begin();
    }
}
