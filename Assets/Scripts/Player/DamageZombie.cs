using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZombie : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<EnemyNavigation>(out var _))
            return;
        if (other.TryGetComponent<StatsHandler>(out var stats))
            stats.TakeDamage(5);
    }
}
