using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    private ZombieSpawn[] portals;
    [SerializeField] private float secToPortal;
    private float timer;

    private void Start()
    {
        portals = GetComponentsInChildren<ZombieSpawn>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > secToPortal)
        {
            timer = 0;
            int rd = Random.Range(0, portals.Length);
            portals[rd].OpenPortal();
        }
    }
}
