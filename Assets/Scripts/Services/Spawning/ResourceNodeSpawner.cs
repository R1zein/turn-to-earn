using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Keeps every resource area filled: each interval one random free point of the
// area gets a random node from that area's list. A mined-out node frees its point.
public class ResourceNodeSpawner : IInitializable, ITickable
{
    [Inject] private DiContainer container;
    [Inject] private LevelAnchors anchors;

    private class AreaState
    {
        public ResourceArea Area;
        public readonly List<int> FreePoints = new List<int>();
        public float Timer;
    }

    private readonly List<AreaState> areas = new List<AreaState>();
    private readonly Dictionary<ResourceController, (AreaState area, int point)> occupied =
        new Dictionary<ResourceController, (AreaState, int)>();

    public void Initialize()
    {
        foreach (ResourceArea area in anchors.ResourceAreas)
        {
            var state = new AreaState { Area = area };
            for (int i = 0; i < area.Points.Count; i++)
                state.FreePoints.Add(i);
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
            slot.area.FreePoints.Add(slot.point);
    }

    private void SpawnAtRandomFreePoint(AreaState state)
    {
        List<int> free = state.FreePoints;
        int index = Random.Range(0, free.Count);
        int point = free[index];
        free[index] = free[free.Count - 1];
        free.RemoveAt(free.Count - 1);

        ResourceArea area = state.Area;
        GameObject prefab = area.NodePrefabs[Random.Range(0, area.NodePrefabs.Count)];
        var node = container.InstantiatePrefabForComponent<ResourceController>(
            prefab, area.Points[point].position, Quaternion.identity, area.transform);
        occupied.Add(node, (state, point));
    }
}
