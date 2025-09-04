using UnityEngine;

public class DamageBot : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if (other.TryGetComponent<StatsHandler>(out var stats))
        {
            if (stats.fraction == Fraction.Friendly)
            {
                stats.TakeDamage(5);
            }
        }
    }
}
