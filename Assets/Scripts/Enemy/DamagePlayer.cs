using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<FirstPersonMovement>(out var _)) 
            return;
        if (other.TryGetComponent<StatsHandler>(out var stats))
            stats.TakeDamage(10);
    }
}

