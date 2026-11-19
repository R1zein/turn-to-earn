using UnityEngine;
using UnityEngine.AI;
using Zenject;

// The one place enemies are born, so that they go through the container.
public class EnemySpawner
{
    [Inject] private DiContainer container;

    public Enemy Spawn(Enemy prefab, Vector3 position) => Spawn(prefab, position, Quaternion.identity);

    public Enemy Spawn(Enemy prefab, Vector3 position, Quaternion rotation) =>
        container.InstantiatePrefabForComponent<Enemy>(prefab, position, rotation, null);

    public void SpawnFromPortal(Enemy prefab, Portal portal)
    {
        Vector3 position = portal.SpawnPosition;
        NavMeshAgent agent = prefab.GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 6f, filter))
        {
            Enemy enemy = Spawn(prefab, hit.position);
            if (portal.ApproachPoint is Vector3 waypoint)
                enemy.GetComponent<NPCNavigation>().Approach(waypoint);
        }
        else
            Debug.LogWarning($"No NavMesh near enemy portal at {position}");
    }
}
