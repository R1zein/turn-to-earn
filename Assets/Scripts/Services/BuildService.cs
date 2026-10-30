using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Rules of building. One ghost at a time; the price is checked when a ghost is
// picked but taken only when the building is actually placed, so an unplaced
// ghost costs nothing.
//
// A building with a GridBuilding goes on the grid: the ghost snaps to cells and
// quarter turns, the grid is drawn, and it can only be placed on free cells. While
// its cells overlap a building, that building is tinted and the cells go red.
// Anything else (the shop table) is placed freely where the ghost stands.
public class BuildService
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BuildingPlacer placer;
    [Inject] private BuildGrid grid;
    [Inject] private BuildGridOverlay overlay;
    [Inject] private WallCorners wallCorners;

    private readonly HashSet<GridBuilding> tinted = new HashSet<GridBuilding>();
    private readonly HashSet<GridBuilding> inConflict = new HashSet<GridBuilding>();

    private GridBuilding gridPrefab;   // null = free placement
    private int quarterTurns;
    private float scrollSum;
    private GridFootprint footprint;
    private bool isAimed;

    public Ghost Current { get; private set; }

    // On the grid: aimed at free cells. Free placement: always.
    public bool CanPlace => gridPrefab == null || (isAimed && inConflict.Count == 0);

    public bool CanAfford(Ghost ghost) => wallet.Current >= ghost.requiredResources;

    public void Pick(Ghost prefab)
    {
        if (Current != null || !CanAfford(prefab))
            return;

        Current = placer.SpawnGhost(prefab);
        gridPrefab = prefab.prefab.GetComponent<GridBuilding>();
        quarterTurns = 0;
        scrollSum = 0f;
        isAimed = false;
    }

    // The player's aim landed here.
    public void Aim(Vector3 point)
    {
        if (Current == null)
            return;

        if (gridPrefab == null)
        {
            Current.transform.position = point;
            return;
        }

        footprint = grid.FootprintAt(point, gridPrefab.SizeTurned(quarterTurns));
        isAimed = true;

        Quaternion rotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
        Vector3 pivot = gridPrefab.PivotFor(grid.Centre(footprint), rotation);
        pivot.y = grid.BaseHeight(footprint);
        Current.transform.SetPositionAndRotation(pivot, rotation);

        ShowConflicts();
        overlay.Show(footprint);
    }

    // Mouse wheel. Free placement turns smoothly; on the grid every notch is a quarter turn.
    public void Turn(float notches, float degreesPerNotch)
    {
        if (Current == null)
            return;

        if (gridPrefab == null)
        {
            Current.transform.Rotate(0f, notches * degreesPerNotch, 0f);
            return;
        }

        scrollSum += notches;
        if (Mathf.Abs(scrollSum) < 1f)
            return;

        quarterTurns = (quarterTurns + (scrollSum > 0f ? 1 : 3)) % 4;
        scrollSum = 0f;
    }

    public bool TryPlaceCurrent()
    {
        if (Current == null || !CanPlace || !wallet.TrySpend(Current.requiredResources))
            return false;

        GameObject placed = placer.Place(Current.prefab, Current.transform.position, Current.transform.rotation);
        if (gridPrefab != null)
        {
            placed.GetComponent<GridBuilding>().Claim(footprint);
            if (placed.TryGetComponent(out WallSegment wall))
                wallCorners.Add(wall);
            ClearConflicts();
            overlay.Hide();
        }

        Object.Destroy(Current.gameObject);
        Current = null;
        gridPrefab = null;
        return true;
    }

    // Collects who stands on the aimed cells and tints exactly them.
    private void ShowConflicts()
    {
        inConflict.Clear();
        foreach (Vector2Int cell in footprint.Cells)
        {
            GridBuilding building = grid.At(cell);
            if (building != null)
                inConflict.Add(building);
        }

        foreach (GridBuilding building in tinted)
            if (building != null && !inConflict.Contains(building))
                building.SetConflict(false);
        foreach (GridBuilding building in inConflict)
            if (!tinted.Contains(building))
                building.SetConflict(true);

        tinted.Clear();
        tinted.UnionWith(inConflict);
    }

    private void ClearConflicts()
    {
        foreach (GridBuilding building in tinted)
            if (building != null)
                building.SetConflict(false);
        tinted.Clear();
        inConflict.Clear();
    }
}
