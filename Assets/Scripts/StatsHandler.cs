using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsHandler : MonoBehaviour
{
    public float maxHP;
    public float currHP;
    public event Action OnDamage;
    public event Action OnDeath;

    public void TakeDamage(float damage)
    {
        currHP -= damage;
        OnDamage?.Invoke();
        if (currHP <= 0 ) 
        {
            OnDeath?.Invoke();
            Destroy(gameObject, 2);
        }
    }

    private void Start()
    {

        currHP = maxHP;
    }


}
