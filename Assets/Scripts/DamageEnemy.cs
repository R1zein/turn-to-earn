using Unity.VisualScripting;
using UnityEngine;

public class DamageEnemy : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(!other.TryGetComponent<Enemy>(out var _)) return;
        if (other.TryGetComponent<StatsHandler>(out var stats))
            stats.TakeDamage(50);
    }


}
