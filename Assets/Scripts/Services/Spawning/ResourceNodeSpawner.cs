using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Keeps every resource area filled: each interval one random free point of the
// area gets a random node from that area's list. A mined-out node frees its point.
public class ResourceNodeSpawner : IInitializable, ITickable
{
    [Inject] private DiContainer container;
    [Inject] private LevelAnchors anchors;
    [Inject] private BuildGrid grid;

    private class AreaState
    {
        public ResourceArea Area;
        public readonly List<SpawnSlot> FreePoints = new List<SpawnSlot>();
        public float Timer;
    }

    private class SpawnSlot
    {
        public Transform Point;
        public GameObject Prefab;
    }

    private readonly List<AreaState> areas = new List<AreaState>();
    private readonly Dictionary<ResourceController, (AreaState area, SpawnSlot slot)> occupied =
        new Dictionary<ResourceController, (AreaState area, SpawnSlot slot)>();

    public void Initialize()
    {
        foreach (ResourceArea area in anchors.ResourceAreas)
        {
            var state = new AreaState { Area = area };
            var bag = new List<GameObject>();
            foreach (Transform point in area.Points)
            {
                GameObject prefab = point.TryGetComponent(out ResourceSpawnPoint explicitPoint) ? explicitPoint.Prefab : null;
                if (prefab == null)
                {
                    if (bag.Count == 0)
                        bag.AddRange(area.NodePrefabs);
                    if (bag.Count == 0)
                        continue;

                    int pick = Random.Range(0, bag.Count);
                    prefab = bag[pick];
                    bag.RemoveAt(pick);
                }

                state.FreePoints.Add(new SpawnSlot { Point = point, Prefab = prefab });
            }
            areas.Add(state);
        }
    }

    public void Tick()
    {
        foreach (AreaState state in areas)
        {
            state.Timer += Time.deltaTime;
            if (state.Timer < state.Area.SpawnInterval)
                continue;
            state.Timer = 0f;

            if (state.FreePoints.Count > 0)
                SpawnAtRandomFreePoint(state);
        }
    }

    // Called by a node when it is mined out.
    public void Release(ResourceController node)
    {
        if (occupied.Remove(node, out var slot))
            slot.area.FreePoints.Add(slot.slot);
    }

    private void SpawnAtRandomFreePoint(AreaState state)
    {
        List<SpawnSlot> free = state.FreePoints;
        int start = Random.Range(0, free.Count);
        for (int offset = 0; offset < free.Count; offset++)
        {
            int index = (start + offset) % free.Count;
            Vector2Int cell = grid.FootprintAt(free[index].Point.position, Vector2Int.one).Min;
            if (grid.At(cell) != null)
                continue;

            Spawn(state, index);
            return;
        }
    }

    private void Spawn(AreaState state, int index)
    {
        List<SpawnSlot> free = state.FreePoints;
        SpawnSlot slot = free[index];
        var node = container.InstantiatePrefabForComponent<ResourceController>(slot.Prefab,
            slot.Point.position, slot.Point.rotation, state.Area.transform);
        free[index] = free[free.Count - 1];
        free.RemoveAt(free.Count - 1);
        occupied.Add(node, (state, slot));
    }
}
