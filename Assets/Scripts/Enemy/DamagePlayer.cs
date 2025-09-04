using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StatsHandler>(out var stats))
        {
            if (stats.fraction == Fraction.Player)
            {
                stats.TakeDamage(10);
            }
        }
    }
}

