using UnityEngine;
using Zenject;

// Taking buildings apart: while on, the placed building under the aim is tinted, and
// confirming removes it and pays its price back. Its cells free up and its neighbours
// close their sides again on their own, as after any destroyed building.
public class Dismantler
{
    [Inject] private BuildService buildService;
    [Inject] private ResourceWallet wallet;

    private GridBuilding target;

    public bool IsActive { get; private set; }

    public void Begin()
    {
        buildService.Cancel();
        IsActive = true;
    }

    public void End()
    {
        SetTarget(null);
        IsActive = false;
    }

    public void Aim(Collider aimed)
    {
        GridBuilding building = aimed != null ? aimed.GetComponentInParent<GridBuilding>() : null;
        SetTarget(building != null && building.IsClaimed ? building : null);
    }

    public void TryDismantle()
    {
        if (target == null)
            return;

        wallet.Add(target.Price);
        Object.Destroy(target.gameObject);
        target = null;
    }

    private void SetTarget(GridBuilding building)
    {
        if (building == target)
            return;

        if (target != null)
            target.SetConflict(false);
        target = building;
        if (target != null)
            target.SetConflict(true);
    }
}
