using UnityEngine;
using Zenject;

// Rules of buying helper bots. The shop panel only forwards the player's choice.
public class ShopService
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BotSpawner spawner;

    public bool TryBuyBot(NPCFacade prefab, Vector3 position)
    {
        if (!wallet.TrySpend(prefab.requiredResources))
            return false;

        spawner.Spawn(prefab, position);
        return true;
    }
}
