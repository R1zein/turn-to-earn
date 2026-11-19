using UnityEngine;
using UnityEngine.AI;
using Zenject;

// The one place enemies are born, so that they go through the container.
public class EnemySpawner
{
    private const int SpawnAttempts = 6;
    [Inject] private DiContainer container;

    public Enemy Spawn(Enemy prefab, Vector3 position) => Spawn(prefab, position, Quaternion.identity);

    public Enemy Spawn(Enemy prefab, Vector3 position, Quaternion rotation) =>
        container.InstantiatePrefabForComponent<Enemy>(prefab, position, rotation, null);

    public void SpawnFromPortal(Enemy prefab, Portal portal)
    {
        Vector3 position = portal.SpawnPosition;
        NPCMotor motor = prefab.GetComponent<NPCMotor>();
        NavMeshHit hit;
        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            Vector2 heading = Random.insideUnitCircle.normalized * Random.Range(1f, 3f);
            Vector3 candidate = position + new Vector3(heading.x, 0f, heading.y);
            if (!motor.TrySample(candidate, 4f, out hit))
                continue;

            float offset = Vector3.ProjectOnPlane(hit.position - position, Vector3.up).sqrMagnitude;
            if (offset is >= 1f and <= 9f)
            {
                SpawnAtPortal(prefab, portal, hit.position);
                return;
            }
        }

        if (motor.TrySample(position, 6f, out hit))
            SpawnAtPortal(prefab, portal, hit.position);
        else
            Debug.LogWarning($"No NavMesh near enemy portal at {position}");
    }

    private void SpawnAtPortal(Enemy prefab, Portal portal, Vector3 point)
    {
        Enemy enemy = Spawn(prefab, point);
        if (portal.ApproachPoint is Vector3 waypoint)
            enemy.GetComponent<NPCMotor>().SetPortalExit(waypoint);
    }
}
