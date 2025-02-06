using UnityEngine;

public class DamageZombie : MonoBehaviour
{
    public float damage;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<Enemy>(out var _))
            return;
        if (other.TryGetComponent<StatsHandler>(out var stats))
            stats.TakeDamage(damage);
    }
}
