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
    [Inject] private WallCorners wallCorners;
    [Inject] private WallStairs wallStairs;

    // The old structure and the one that took its place.
    public event Action<Structure, Structure> OnUpgraded;

    public bool CanAfford(StructureUpgrade upgrade) => wallet.Current >= upgrade.Price;

    // The first of the upgrade's results that fits where the structure stands, or null.
    public Structure Fitting(Structure structure, StructureUpgrade upgrade)
    {
        GridBuilding standing = structure.GetComponent<GridBuilding>();
        foreach (Structure result in upgrade.Results)
            if (standing == null || Fits(result.GetComponent<GridBuilding>(), standing))
                return result;
        return null;
    }

    public bool TryUpgrade(Structure structure, StructureUpgrade upgrade)
    {
        Structure result = Fitting(structure, upgrade);
        if (result == null || !wallet.TrySpend(upgrade.Price))
            return false;

        Transform body = structure.transform;
        GridBuilding standing = structure.GetComponent<GridBuilding>();
        StatsHandler stats = structure.Stats;
        float damage = stats.maxHP - stats.currHP;

        // The old one lets go of its cells and joints now, not when it is destroyed at
        // the end of the frame, so the new one finds them free.
        if (standing != null)
            standing.Leave();
        if (structure.TryGetComponent(out WallSegment wall))
        {
            wallCorners.Remove(wall);
            wallStairs.Remove(wall);
        }
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
