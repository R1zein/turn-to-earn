using Zenject;

// Rules of building. The price is checked when a ghost is picked but taken only
// when the building is actually placed, so an unplaced ghost costs nothing.
public class BuildService
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BuildingPlacer placer;

    public bool CanAfford(Ghost ghost) => wallet.Current >= ghost.requiredResources;

    public Ghost StartPlacing(Ghost prefab) =>
        CanAfford(prefab) ? placer.SpawnGhost(prefab) : null;

    public bool TryPlace(Ghost ghost)
    {
        if (!wallet.TrySpend(ghost.requiredResources))
            return false;

        placer.Place(ghost.prefab, ghost.transform.position, ghost.transform.rotation);
        return true;
    }
}
