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

    // Turrets and damage zones keep hitting a body until it is destroyed;
    // without this flag every such hit would raise OnDeath again.
    public bool IsDead { get; private set; }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        currHP -= damage;
        OnDamage?.Invoke();
        if (currHP <= 0)
        {
            IsDead = true;
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