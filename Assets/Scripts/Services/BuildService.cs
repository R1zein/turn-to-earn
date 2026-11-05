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
// Near a free socket of a placed building the ghost is drawn to it, so a wall puts
// its palisade to the middle of a tower and a tower its middle to a wall's end. Its
// height can be nudged by one step either way.
// Anything else (the shop table) is placed freely where the ghost stands.
public class BuildService
{
    [Inject] private ResourceWallet wallet;
    [Inject] private BuildingPlacer placer;
    [Inject] private BuildGrid grid;
    [Inject] private BuildGridOverlay overlay;
    [Inject] private WallCorners wallCorners;
    [Inject] private WallStairs wallStairs;
    [Inject] private GameConfig config;

    private readonly HashSet<GridBuilding> tinted = new HashSet<GridBuilding>();
    private readonly HashSet<GridBuilding> inConflict = new HashSet<GridBuilding>();

    private GridBuilding gridPrefab;   // null = free placement
    private int quarterTurns;
    private float scrollSum;
    private GridFootprint footprint;
    private bool isAimed;

    public Ghost Current { get; private set; }

    public bool IsOnGrid => gridPrefab != null;

    // -1, 0 or 1 steps of GameConfig.BuildHeightStep from where the building would stand.
    public int HeightSteps { get; private set; }

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
        HeightSteps = 0;
    }

    // Puts the ghost away without building anything.
    public void Cancel()
    {
        if (Current == null)
            return;

        if (gridPrefab != null)
        {
            ClearConflicts();
            overlay.Hide();
        }
        Object.Destroy(Current.gameObject);
        Current = null;
        gridPrefab = null;
    }

    public void ChangeHeight(int steps)
    {
        if (gridPrefab != null)
            HeightSteps = Mathf.Clamp(HeightSteps + steps, -1, 1);
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

        footprint = SnappedFootprint(point, out GridBuilding joined);
        isAimed = true;

        Quaternion rotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
        Vector3 pivot = gridPrefab.PivotFor(grid.Centre(footprint), rotation);
        pivot.y = StandingHeight(joined) + HeightSteps * config.BuildHeightStep;
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
            placed.GetComponent<GridBuilding>().Claim(footprint, quarterTurns, Current.requiredResources);
            if (placed.TryGetComponent(out WallSegment wall))
            {
                wallCorners.Add(wall);
                wallStairs.Add(wall);
            }
            ClearConflicts();
            overlay.Hide();
        }

        Object.Destroy(Current.gameObject);
        Current = null;
        gridPrefab = null;
        return true;
    }

    // The nearest footprint that joins a free socket of a placed building, within
    // GameConfig.BuildSnapDistance of the aim; if there is none, the cells under the aim.
    private GridFootprint SnappedFootprint(Vector3 point, out GridBuilding joined)
    {
        Vector2Int size = gridPrefab.SizeTurned(quarterTurns);
        GridFootprint best = grid.FootprintAt(point, size);
        float bestDistance = config.BuildSnapDistance;
        joined = null;

        foreach (GridBuilding building in grid.Placed)
        {
            foreach (GridSocket theirs in building.Sockets)
            {
                if (building.JoinedAt(theirs) != null)
                    continue;

                Vector2Int outward = building.SocketOutward(theirs);
                foreach (GridSocket ours in gridPrefab.Sockets)
                {
                    if (GridBuilding.TurnedOutward(ours, quarterTurns) != -outward)
                        continue;

                    Vector2Int min = building.SocketCell(theirs) + outward - gridPrefab.TurnedCell(ours, quarterTurns);
                    var candidate = new GridFootprint(min, size);
                    Vector3 centre = grid.Centre(candidate);
                    float distance = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(centre.x, centre.z));
                    if (distance < bestDistance && IsFree(candidate))
                    {
                        best = candidate;
                        bestDistance = distance;
                        joined = building;
                    }
                }
            }
        }
        return best;
    }

    private bool IsFree(GridFootprint cells)
    {
        foreach (Vector2Int cell in cells.Cells)
            if (grid.At(cell) != null)
                return false;
        return true;
    }

    // On the ground - or level with the building it joins, if that is only a step
    // away: walkways then meet, where a step of ground would otherwise break them.
    private float StandingHeight(GridBuilding joined)
    {
        float ground = grid.BaseHeight(footprint);
        if (joined == null)
            return ground;

        float level = joined.transform.position.y;
        return Mathf.Abs(level - ground) <= config.BuildHeightStep + 0.01f ? level : ground;
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
