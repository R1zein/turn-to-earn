using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotMiner : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<ResourceController>();
    }

    protected override void Navigation()
    {
        navigation.ChaseTarget<ResourceController>();
    }
}
