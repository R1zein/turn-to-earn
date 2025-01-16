using UnityEngine;

public class Player : MonoBehaviour
{
    private StatsHandler statsHandler;

    void Awake()
    {
        statsHandler = GetComponent<StatsHandler>();
    }
    private void Death()
    {
         GetComponent<FirstPersonMovement>().enabled = false;
    }
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
    }

    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
    }
}
