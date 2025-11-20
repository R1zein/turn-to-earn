using UnityEngine;
using UnityEngine.UI;

public class PhpBarController : MonoBehaviour
{
    public Slider slider;
    private StatsHandler statsHandler;
    private void Awake()
    {
        statsHandler = GetComponent<StatsHandler>();

    }

    private void UpdateUI()
    {
        slider.value = statsHandler.currHP / statsHandler.maxHP;
    }
    private void OnEnable()
    {
        statsHandler.OnDamage += UpdateUI;
    }
    private void OnDisable()
    {
        statsHandler.OnDamage -= UpdateUI;
    }
}
