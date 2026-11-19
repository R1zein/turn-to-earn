using UnityEngine;

// A point can pin a resource type. Empty points use the area's distribution.
public class ResourceSpawnPoint : MonoBehaviour
{
    [SerializeField] private ResourceController prefab;

    public GameObject Prefab => prefab != null ? prefab.gameObject : null;
}
