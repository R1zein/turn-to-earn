using UnityEngine;
using Zenject;

// The one place enemies are born, so that they go through the container.
public class EnemySpawner
{
    [Inject] private DiContainer container;

    public Enemy Spawn(Enemy prefab, Vector3 position) => Spawn(prefab, position, Quaternion.identity);

    public Enemy Spawn(Enemy prefab, Vector3 position, Quaternion rotation) =>
        container.InstantiatePrefabForComponent<Enemy>(prefab, position, rotation, null);
}
