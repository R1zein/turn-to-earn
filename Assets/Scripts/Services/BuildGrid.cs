using System.Collections.Generic;
using UnityEngine;
using Zenject;

// A rectangle of grid cells: what a building stands on.
public readonly struct GridFootprint
{
    public readonly Vector2Int Min;
    public readonly Vector2Int Size;

    public GridFootprint(Vector2Int min, Vector2Int size)
    {
        Min = min;
        Size = size;
    }

    public IEnumerable<Vector2Int> Cells
    {
        get
        {
            for (int x = 0; x < Size.x; x++)
                for (int y = 0; y < Size.y; y++)
                    yield return new Vector2Int(Min.x + x, Min.y + y);
        }
    }

    public bool Contains(Vector2Int cell) =>
        cell.x >= Min.x && cell.x < Min.x + Size.x && cell.y >= Min.y && cell.y < Min.y + Size.y;
}

// The building grid laid over the terrain: world <-> cell, and who stands where.
// Cells are GameConfig.BuildCellSize square and map X/Z of the world to x/y.
public class BuildGrid
{
    [Inject] private GameConfig config;
    [Inject] private Terrain terrain;

    private readonly Dictionary<Vector2Int, GridBuilding> occupied = new Dictionary<Vector2Int, GridBuilding>();
    private readonly List<GridBuilding> placed = new List<GridBuilding>();

    public float CellSize => config.BuildCellSize;

    public IReadOnlyList<GridBuilding> Placed => placed;

    // Grows with every change of occupancy, so views know when to redraw.
    public int Version { get; private set; }

    // The footprint of `size` cells that sits closest to a world point.
    public GridFootprint FootprintAt(Vector3 point, Vector2Int size) => new GridFootprint(
        new Vector2Int(
            Mathf.RoundToInt(point.x / CellSize - size.x * 0.5f),
            Mathf.RoundToInt(point.z / CellSize - size.y * 0.5f)),
        size);

    public Vector3 Centre(GridFootprint footprint) => new Vector3(
        (footprint.Min.x + footprint.Size.x * 0.5f) * CellSize,
        0f,
        (footprint.Min.y + footprint.Size.y * 0.5f) * CellSize);

    public float GroundHeight(float x, float z) =>
        terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;

    // Where a building on this footprint stands: the lowest ground under it, so it
    // never hangs in the air, snapped to BuildHeightStep, so neighbours on almost
    // level ground come out exactly level and their tops line up.
    public float BaseHeight(GridFootprint footprint)
    {
        float lowest = float.PositiveInfinity;
        for (int x = 0; x <= footprint.Size.x; x++)
            for (int y = 0; y <= footprint.Size.y; y++)
                lowest = Mathf.Min(lowest, GroundHeight((footprint.Min.x + x) * CellSize, (footprint.Min.y + y) * CellSize));

        float step = config.BuildHeightStep;
        return Mathf.Floor(lowest / step) * step;
    }

    public GridBuilding At(Vector2Int cell) => occupied.TryGetValue(cell, out GridBuilding building) ? building : null;

    public void Occupy(GridBuilding building, GridFootprint footprint)
    {
        foreach (Vector2Int cell in footprint.Cells)
            occupied[cell] = building;
        placed.Add(building);
        OnChanged();
    }

    public void Release(GridBuilding building, GridFootprint footprint)
    {
        foreach (Vector2Int cell in footprint.Cells)
            if (At(cell) == building)
                occupied.Remove(cell);
        placed.Remove(building);
        OnChanged();
    }

    // Who joins whom may have changed, and with it which sides stand open.
    private void OnChanged()
    {
        Version++;
        foreach (GridBuilding building in placed)
            if (building != null)
                building.RefreshSockets();
    }
}
