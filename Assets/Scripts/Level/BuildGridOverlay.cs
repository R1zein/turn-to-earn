using System.Collections.Generic;
using UnityEngine;
using Zenject;

// The grid drawn on the terrain while building: one mesh of cells hugging the
// ground around the aimed building, coloured by state. The gaps between the cells
// are the grid lines. A cell where the new building hits another one lies under
// that building, so it also gets a red column around it, visible from any side.
// Rebuilt only when what it shows has changed.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BuildGridOverlay : MonoBehaviour
{
    private const float LineWidth = 0.05f;
    private const float Lift = 0.12f;
    // The column rises above the building it marks, and is a hair wider than a cell,
    // or it would stay hidden inside a mesh wider than its cells (a tower).
    private const float ColumnClearance = 0.25f;
    private const float ColumnMargin = 0.14f;

    [Inject] private BuildGrid grid;
    [Inject] private GameConfig config;

    [SerializeField] private Color freeColor = new Color(1f, 1f, 1f, 0.03f);
    [Tooltip("Cells other buildings stand on")]
    [SerializeField] private Color occupiedColor = new Color(1f, 0.6f, 0.1f, 0.2f);
    [Tooltip("Cells of the new building, where it fits")]
    [SerializeField] private Color footprintColor = new Color(0.2f, 1f, 0.4f, 0.35f);
    [Tooltip("Cells where the new building hits another one")]
    [SerializeField] private Color conflictColor = new Color(1f, 0.06f, 0.04f, 0.28f);

    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();

    private Mesh mesh;
    private MeshRenderer view;
    private GridFootprint shown;
    private int shownVersion = -1;

    private void Awake()
    {
        mesh = new Mesh { name = "BuildGrid" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        view = GetComponent<MeshRenderer>();
        view.enabled = false;
    }

    private void OnDestroy()
    {
        Destroy(mesh);
    }

    public void Show(GridFootprint footprint)
    {
        view.enabled = true;
        if (shownVersion == grid.Version && shown.Min == footprint.Min && shown.Size == footprint.Size)
            return;

        shown = footprint;
        shownVersion = grid.Version;
        Rebuild(footprint);
    }

    public void Hide()
    {
        view.enabled = false;
        shownVersion = -1;
    }

    private void Rebuild(GridFootprint footprint)
    {
        vertices.Clear();
        colors.Clear();
        triangles.Clear();

        int radius = config.BuildGridRadius;
        Vector2Int from = footprint.Min - Vector2Int.one * radius;
        Vector2Int to = footprint.Min + footprint.Size + Vector2Int.one * radius;
        for (int x = from.x; x < to.x; x++)
            for (int y = from.y; y < to.y; y++)
                AddCell(new Vector2Int(x, y), CellColor(new Vector2Int(x, y), footprint));

        foreach (Vector2Int cell in footprint.Cells)
            if (grid.At(cell) != null)
                AddColumn(cell, grid.At(cell).Top);

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    private Color CellColor(Vector2Int cell, GridFootprint footprint)
    {
        bool isTaken = grid.At(cell) != null;
        if (footprint.Contains(cell))
            return isTaken ? conflictColor : footprintColor;
        return isTaken ? occupiedColor : freeColor;
    }

    private void AddColumn(Vector2Int cell, float buildingTop)
    {
        float size = grid.CellSize;
        float x0 = cell.x * size - ColumnMargin;
        float x1 = (cell.x + 1) * size + ColumnMargin;
        float z0 = cell.y * size - ColumnMargin;
        float z1 = (cell.y + 1) * size + ColumnMargin;
        float bottom = grid.GroundHeight((x0 + x1) * 0.5f, (z0 + z1) * 0.5f) - 1f;
        float top = buildingTop + ColumnClearance;

        var a = new Vector3(x0, bottom, z0);
        var b = new Vector3(x1, bottom, z0);
        var c = new Vector3(x1, bottom, z1);
        var d = new Vector3(x0, bottom, z1);
        Vector3 up = Vector3.up * (top - bottom);

        AddQuad(a, b, b + up, a + up);
        AddQuad(b, c, c + up, b + up);
        AddQuad(c, d, d + up, c + up);
        AddQuad(d, a, a + up, d + up);
        AddQuad(a + up, b + up, c + up, d + up);
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        for (int i = 0; i < 4; i++)
            colors.Add(conflictColor);

        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 3);
    }

    private void AddCell(Vector2Int cell, Color color)
    {
        float size = grid.CellSize;
        float x0 = cell.x * size + LineWidth * 0.5f;
        float z0 = cell.y * size + LineWidth * 0.5f;
        float x1 = (cell.x + 1) * size - LineWidth * 0.5f;
        float z1 = (cell.y + 1) * size - LineWidth * 0.5f;

        // A cell is about one terrain sample across, so a flat quad on its four
        // corners follows the ground.
        int first = vertices.Count;
        vertices.Add(new Vector3(x0, grid.GroundHeight(x0, z0) + Lift, z0));
        vertices.Add(new Vector3(x0, grid.GroundHeight(x0, z1) + Lift, z1));
        vertices.Add(new Vector3(x1, grid.GroundHeight(x1, z0) + Lift, z0));
        vertices.Add(new Vector3(x1, grid.GroundHeight(x1, z1) + Lift, z1));
        for (int i = 0; i < 4; i++)
            colors.Add(color);

        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first + 1);
        triangles.Add(first + 3);
        triangles.Add(first + 2);
    }
}
