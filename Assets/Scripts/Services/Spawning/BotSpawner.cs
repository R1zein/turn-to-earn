using UnityEngine;
using Zenject;

// The one place helper bots are born, so that they go through the container.
public class BotSpawner
{
    [Inject] private DiContainer container;

    public NPCFacade Spawn(NPCFacade prefab, Vector3 position) =>
        container.InstantiatePrefabForComponent<NPCFacade>(prefab, position, Quaternion.identity, null);
}
