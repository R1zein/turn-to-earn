using UnityEngine;
using Zenject;

// Rules of building. One ghost at a time; the price is checked when a ghost is
// picked but taken only when the building is actually placed, so an unplaced
// ghost costs nothing.
public class BuildService
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BuildingPlacer placer;

    public Ghost Current { get; private set; }

    public bool CanAfford(Ghost ghost) => wallet.Current >= ghost.requiredResources;

    public void Pick(Ghost prefab)
    {
        if (Current != null || !CanAfford(prefab))
            return;

        Current = placer.SpawnGhost(prefab);
    }

    public bool TryPlaceCurrent()
    {
        if (Current == null || !wallet.TrySpend(Current.requiredResources))
            return false;

        placer.Place(Current.prefab, Current.transform.position, Current.transform.rotation);
        Object.Destroy(Current.gameObject);
        Current = null;
        return true;
    }
}
