using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using System.Threading.Tasks;

public class ZombieSpawn : MonoBehaviour
{
    [SerializeField] private GameObject zombie;
    [SerializeField] private Transform spawnPos;
    [SerializeField] private float spawnTime;
    [SerializeField] private int spawnCount;
    [SerializeField] private TimePeriod timePeriod;
    [SerializeField] private GameObject portalEffect;

    private void OnEnable()
    {
        timePeriod.OnPeriodEnter += OpenPortal;
    }
    private void OnDisable()
    {
        timePeriod.OnPeriodEnter -= OpenPortal;
    }
    public async void OpenPortal()
    {
        portalEffect.SetActive(true);
        for (int i = 0; i < spawnCount; i++)
        {
            Instantiate(zombie, spawnPos.position, Quaternion.identity);
            await Awaitable.WaitForSecondsAsync(spawnTime);  
        }
        portalEffect.SetActive(false);
    }


}
