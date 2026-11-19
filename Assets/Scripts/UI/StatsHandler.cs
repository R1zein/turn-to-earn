using System;
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

    // Repair: never above maxHP, and the dead stay dead.
    public void Heal(float amount)
    {
        if (!IsDead)
            currHP = Mathf.Min(currHP + amount, maxHP);
    }

    // A body that takes another's place (an upgraded wall) starts with what that one
    // had left instead of full health. Called right after it is born, before Start.
    public void StartWith(float hp)
    {
        currHP = hp;
    }

    private void Awake() => currHP = maxHP;

}

public enum Fraction
{
    Player,
    Friendly,
    Enemy,
    Neutral,
}
