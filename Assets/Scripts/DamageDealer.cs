using UnityEngine;

// A weapon trigger: hurts what it touches once on contact, if it belongs to the
// target fraction. Friend or foe is decided only by fraction.
public class DamageDealer : MonoBehaviour
{
    [SerializeField] private Fraction targetFraction;
    [SerializeField] private float damage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StatsHandler>(out var stats) && stats.fraction == targetFraction)
            stats.TakeDamage(damage);
    }
}
