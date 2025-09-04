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
    public Fraction fraction;

    public void TakeDamage(float damage)
    {
        currHP -= damage;
        OnDamage?.Invoke();
        if (currHP <= 0 ) 
        {
            OnDeath?.Invoke();
        }
    }

    private void Start()
    {
        currHP = maxHP;
    }

}

public enum Fraction
{
    Player,
    Friendly,
    Enemy,
    Neutral,
}