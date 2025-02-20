using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BotMiner : NPCFacade
{
    public float mineDistance = 5;
    private float timer;
    private float targetDistance;
    public float cooldown;

    private void Start()
    {
        navigation.FirstLook<ResourceController>(10);
        navigation.SetAndRefresh();
    }

    protected void Update()
    {
        base.Update();
        timer += Time.deltaTime;
        if (navigation.target != null)
        {
            targetDistance = Vector3.Distance(transform.position, navigation.target.position);
            if (targetDistance <= mineDistance & timer >= cooldown)
            {
                timer = 0;
                BotMineResource();
                animator.SetBool("Run", false);
                animator.SetBool("Idle", true);
            }
        }
    }

    protected override void NPCAnimationControl()
    {
        if (targetDistance > mineDistance)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Run", true);
        }
    }
    private void BotMineResource()
    {
        var resource = navigation.target.GetComponent<ResourceController>();
        resource.TakeHit();
        animator.SetTrigger("Mine");
    }

    protected override void Navigation()
    {
        navigation.ChaseTarget<ResourceController>(10);
        navigation.SetAndRefresh();

    }
}
