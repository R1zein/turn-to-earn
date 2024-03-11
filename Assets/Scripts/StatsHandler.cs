using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsHandler : MonoBehaviour
{
    public float maxHP;
    private float currHP;

    public void TakeDamage(float damage)
    {
        currHP -= damage;
        if (currHP <= 0 ) 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currHP = maxHP;
    }

}
