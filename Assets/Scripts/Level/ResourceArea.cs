using System.Collections.Generic;
using UnityEngine;

// A patch of the map where resource nodes grow: its children are the points,
// the prefab list says what grows here. Filling the points is ResourceNodeSpawner's job.
public class ResourceArea : MonoBehaviour
{
    // Field names and types kept from the old ResourceSpawner so the scene data carries over.
    // Each prefab has a ResourceController on its root.
    [SerializeField] private List<GameObject> stonePrefabs = new List<GameObject>();
    [Tooltip("Seconds between attempts to fill a free point")]
    [SerializeField] private int timer;

    private Transform[] points;

    public IReadOnlyList<GameObject> NodePrefabs => stonePrefabs;
    public float SpawnInterval => timer;
    public IReadOnlyList<Transform> Points => points;

    // Captured before any node is spawned: nodes are parented here too.
    private void Awake()
    {
        points = new Transform[transform.childCount];
        for (int i = 0; i < points.Length; i++)
            points[i] = transform.GetChild(i);
    }
}
