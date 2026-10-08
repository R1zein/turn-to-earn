using UnityEngine;
using Zenject;

// The one place buildings and their ghosts are born. Both go through the
// container: the shop table injects ShopController, and the table ghost nests a Table.
public class BuildingPlacer
{
    [Inject] private DiContainer container;

    public Ghost SpawnGhost(Ghost prefab) =>
        container.InstantiatePrefabForComponent<Ghost>(prefab);

    public GameObject Place(GameObject prefab, Vector3 position, Quaternion rotation) =>
        container.InstantiatePrefab(prefab, position, rotation, null);
}
