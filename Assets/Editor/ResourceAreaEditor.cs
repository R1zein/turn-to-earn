using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ResourceArea))]
public class ResourceAreaEditor : Editor
{
    private const int MaxGeneratedPoints = 200;
    private bool isPlacing;
    private Vector2 gridSize = new Vector2(24f, 24f);
    private float spacing = 4f;
    private float maxSlope = 30f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        isPlacing = GUILayout.Toggle(isPlacing, "Place points in Scene view", "Button");
        if (isPlacing)
            EditorGUILayout.HelpBox("Shift + left click on terrain to add a spawn point.", MessageType.Info);

        gridSize = EditorGUILayout.Vector2Field("Grid width and depth", gridSize);
        spacing = Mathf.Max(1f, EditorGUILayout.FloatField("Point spacing", spacing));
        maxSlope = EditorGUILayout.Slider("Maximum grid slope", maxSlope, 0f, 60f);
        if (GUILayout.Button("Add terrain-aligned grid"))
            AddGrid((ResourceArea)target);
    }

    private void OnSceneGUI()
    {
        Event current = Event.current;
        if (!isPlacing || current.type != EventType.MouseDown || current.button != 0 || !current.shift)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 10000f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) && hit.collider is TerrainCollider)
        {
            AddPoint((ResourceArea)target, hit.point);
            current.Use();
        }
    }

    private void AddGrid(ResourceArea area)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Resource points", "Open a scene with an active terrain.", "OK");
            return;
        }

        if (gridSize.x < 0f || gridSize.y < 0f)
            return;

        int columns = Mathf.FloorToInt(gridSize.x / spacing) + 1;
        int rows = Mathf.FloorToInt(gridSize.y / spacing) + 1;
        if ((long)columns * rows > MaxGeneratedPoints)
        {
            EditorUtility.DisplayDialog("Resource points", "Increase spacing to generate at most 200 points at once.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Add resource point grid");
        Vector3 min = terrain.GetPosition();
        TerrainData data = terrain.terrainData;
        Vector3 max = min + data.size;
        var existing = new System.Collections.Generic.List<Vector2>();
        foreach (Transform child in area.transform)
            existing.Add(new Vector2(child.position.x, child.position.z));

        for (int z = 0; z < rows; z++)
        for (int x = 0; x < columns; x++)
        {
            Vector3 point = area.transform.TransformPoint(new Vector3(
                (x - (columns - 1) * 0.5f) * spacing, 0f, (z - (rows - 1) * 0.5f) * spacing));
            var planar = new Vector2(point.x, point.z);
            if (point.x < min.x || point.x >= max.x || point.z < min.z || point.z >= max.z
                || existing.Exists(other => (planar - other).sqrMagnitude < spacing * spacing * 0.81f))
                continue;

            float xFraction = (point.x - min.x) / data.size.x;
            float zFraction = (point.z - min.z) / data.size.z;
            if (data.GetSteepness(xFraction, zFraction) > maxSlope)
                continue;

            point.y = min.y + terrain.SampleHeight(point);
            AddPoint(area, point);
            existing.Add(planar);
        }
        Undo.CollapseUndoOperations(undoGroup);
    }

    private void AddPoint(ResourceArea area, Vector3 position)
    {
        var point = new GameObject("Resource Point");
        point.transform.SetParent(area.transform);
        point.transform.SetPositionAndRotation(position, Quaternion.identity);
        point.AddComponent<ResourceSpawnPoint>();
        Undo.RegisterCreatedObjectUndo(point, "Add resource point");
    }
}
