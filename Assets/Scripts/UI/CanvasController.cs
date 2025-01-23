using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour
{
    public Image hpBar;
    public StatsHandler statsHandler;

    private void ChangeHPBar()
    {
        hpBar.fillAmount = statsHandler.currHP/statsHandler.maxHP;
    }

    private void OnEnable()
    {
        statsHandler.OnDamage += ChangeHPBar;
    }

    private void OnDisable()
    {
        statsHandler.OnDamage -= ChangeHPBar;
    }



    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
