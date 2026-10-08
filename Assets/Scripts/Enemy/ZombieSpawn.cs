using UnityEngine;
using Zenject;

public class ZombieSpawn : MonoBehaviour
{
    [SerializeField] private GameObject zombie;
    [SerializeField] private Transform spawnPos;
    [SerializeField] private float spawnTime;
    [SerializeField] private int spawnCount;
    [SerializeField] private TimePeriod timePeriod;
    [SerializeField] private GameObject portalEffect;

    [Inject] private DayCycle dayCycle;

    private void OnEnable()
    {
        dayCycle.OnPeriodEnter += OnPeriodEnter;
    }
    private void OnDisable()
    {
        dayCycle.OnPeriodEnter -= OnPeriodEnter;
    }

    private void OnPeriodEnter(TimePeriod period)
    {
        if (period == timePeriod)
            OpenPortal();
    }

    public async void OpenPortal()
    {
        portalEffect.SetActive(true);
        for (int i = 0; i < spawnCount + dayCycle.EnterCount(timePeriod) * 2; i++)
        {
            Instantiate(zombie, spawnPos.position, Quaternion.identity);
            await Awaitable.WaitForSecondsAsync(spawnTime);
        }
        portalEffect.SetActive(false);
    }
}
