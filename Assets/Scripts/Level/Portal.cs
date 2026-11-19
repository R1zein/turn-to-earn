using UnityEngine;
using Unity.AI.Navigation;

// Body of a zombie portal: where enemies appear and the effect shown while they do.
// When and how many is WaveService's decision.
public class Portal : MonoBehaviour
{
    [SerializeField] private Transform spawnPos;
    [SerializeField] private GameObject portalEffect;
    [SerializeField] private NavMeshLink exitLink;

    public Vector3 SpawnPosition => spawnPos.position;
    public Vector3? ApproachPoint => exitLink != null ? exitLink.transform.TransformPoint(exitLink.endPoint) : null;

    public void SetOpen(bool isOpen) => portalEffect.SetActive(isOpen);
}
