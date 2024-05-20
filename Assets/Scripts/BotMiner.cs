using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotMiner : MonoBehaviour
{
    private StatsHandler stats;
    private void Awake()
    {
        stats = GetComponent<StatsHandler>();
    }
    private void OnEnable()
    {
        stats.OnDeath += Death;
    }

    private void OnDisable()
    {
        stats.OnDeath -= Death;
    }

    private void Death()
    {
        Destroy(gameObject, 3f);
    }

}
