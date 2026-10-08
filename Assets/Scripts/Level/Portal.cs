using UnityEngine;

// Body of a zombie portal: where enemies appear and the effect shown while they do.
// When and how many is WaveService's decision.
public class Portal : MonoBehaviour
{
    [SerializeField] private Transform spawnPos;
    [SerializeField] private GameObject portalEffect;

    public Vector3 SpawnPosition => spawnPos.position;

    public void SetOpen(bool isOpen) => portalEffect.SetActive(isOpen);
}
