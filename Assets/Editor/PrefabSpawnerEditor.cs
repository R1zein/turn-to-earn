using UnityEngine;
using UnityEditor;

public class PrefabSpawnerEditor : EditorWindow
{
    GameObject prefab;
    Terrain terrain;
    Vector2 areaCenter = Vector2.zero;
    Vector2 areaSize = new Vector2(10, 10);
    int count = 10;

    [MenuItem("Tools/Prefab Spawner on Terrain")]
    static void Init()
    {
        PrefabSpawnerEditor window = (PrefabSpawnerEditor)EditorWindow.GetWindow(typeof(PrefabSpawnerEditor));
        window.titleContent = new GUIContent("Terrain Spawner");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Spawn On Terrain", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        areaCenter = EditorGUILayout.Vector2Field("Area Center (X,Z)", areaCenter);
        areaSize = EditorGUILayout.Vector2Field("Area Size", areaSize);
        count = EditorGUILayout.IntField("Count", count);

        if (GUILayout.Button("Spawn"))
        {
            if (prefab == null || terrain == null)
            {
                Debug.LogError("Assign both prefab and terrain.");
                return;
            }

            SpawnPrefabs();
        }
    }

    void SpawnPrefabs()
    {
        Undo.RegisterSceneUndo("Spawn Prefabs On Terrain");

        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        for (int i = 0; i < count; i++)
        {
            float randX = Random.Range(-areaSize.x / 2f, areaSize.x / 2f) + areaCenter.x;
            float randZ = Random.Range(-areaSize.y / 2f, areaSize.y / 2f) + areaCenter.y;

            float worldX = terrainPos.x + randX;
            float worldZ = terrainPos.z + randZ;
            float height = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

            Vector3 spawnPos = new Vector3(worldX, height, worldZ);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = spawnPos;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn On Terrain");
        }
    }
}

