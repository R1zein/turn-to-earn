using System;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

// Turns a placed structure into another in the same place, instead of dismantling it
// and building the new one by hand. The new one stands on the same cells with the
// same turn, joins the same neighbours, keeps the damage the old one had taken, and
// counts both prices as paid, so dismantling it pays back everything.
// A result on the grid needs its cells free, except those the old one stands on.
public class StructureUpgrades
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BuildGrid grid;
    [Inject] private BuildingPlacer placer;
    [Inject] private BuildService buildService;

    // The old structure and the one that took its place.
    public event Action<Structure, Structure> OnUpgraded;

    public bool CanAfford(StructureUpgrade upgrade) => wallet.Current >= upgrade.Price;

    public bool IsOffered(Structure structure, StructureUpgrade upgrade)
    {
        if (upgrade.Action == StructureUpgradeAction.Replace)
            return true;

        return structure.TryGetComponent(out TowerMount mount)
            && mount.HasWeapon == (upgrade.Action == StructureUpgradeAction.RemoveMount);
    }

    // The first of the upgrade's results that fits where the structure stands, or null.
    public Structure Fitting(Structure structure, StructureUpgrade upgrade)
    {
        if (!IsOffered(structure, upgrade) || structure.Stats.IsDead)
            return null;

        GridBuilding standing = structure.GetComponent<GridBuilding>();
        if (upgrade.Action != StructureUpgradeAction.Replace)
            return standing != null && standing.IsClaimed ? upgrade.Result : null;

        foreach (Structure result in upgrade.Results)
            if (standing == null || (result.TryGetComponent(out GridBuilding gridResult) && Fits(gridResult, standing)))
                return result;
        return null;
    }

    public bool TryUpgrade(Structure structure, StructureUpgrade upgrade)
    {
        if (structure == null || !structure.isActiveAndEnabled)
            return false;

        Structure result = Fitting(structure, upgrade);
        if (result == null || !wallet.TrySpend(upgrade.Price))
            return false;

        if (upgrade.Action != StructureUpgradeAction.Replace)
        {
            var mount = structure.GetComponent<TowerMount>();
            if (upgrade.Action == StructureUpgradeAction.RemoveMount)
                mount.RemoveWeapon();
            else
            {
                Transform socket = mount.Socket;
                GameObject weapon = placer.Place(result.gameObject, socket.position, socket.rotation);
                mount.Install(weapon.GetComponent<Structure>(), upgrade.Price);
            }
            return true;
        }

        Transform body = structure.transform;
        GridBuilding standing = structure.GetComponent<GridBuilding>();
        StatsHandler stats = structure.Stats;
        float damage = stats.maxHP - stats.currHP;

        // The old one lets go of its cells and joints now, not when it is destroyed at
        // the end of the frame, so the new one finds them free.
        structure.gameObject.SetActive(false);
        Object.Destroy(structure.gameObject);

        GameObject placed = placer.Place(result.gameObject, body.position, body.rotation);
        if (standing != null)
            buildService.Settle(placed.GetComponent<GridBuilding>(), standing.Footprint,
                standing.QuarterTurns, standing.Price + upgrade.Price);
        StatsHandler placedStats = placed.GetComponent<StatsHandler>();
        placedStats.StartWith(Mathf.Max(placedStats.maxHP - damage, 1f));

        OnUpgraded?.Invoke(structure, placed.GetComponent<Structure>());
        return true;
    }

    private bool Fits(GridBuilding result, GridBuilding standing)
    {
        foreach (Vector2Int cell in result.CellsAt(standing.Footprint, standing.QuarterTurns))
        {
            GridBuilding occupant = grid.At(cell);
            if (occupant != null && occupant != standing)
                return false;
        }
        return true;
    }
}
