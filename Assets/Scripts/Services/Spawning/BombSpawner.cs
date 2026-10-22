using UnityEngine;
using Zenject;

// The one place bombs are born, so that they go through the container.
public class BombSpawner
{
    [Inject] private DiContainer container;

    public DroneBomb Spawn(DroneBomb prefab, Vector3 position, Quaternion rotation) =>
        container.InstantiatePrefabForComponent<DroneBomb>(prefab, position, rotation, null);
}
