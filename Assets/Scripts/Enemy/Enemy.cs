using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NPCFacade
{
    private NPCNavigation navigation;
    private void Start()
    {
        navigation = GetComponent<NPCNavigation>();
        navigation.FirstLook<BotNavigation>();
        navigation.FirstLook<BotDefender>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotNavigation>();
        navigation.ChaseTarget<BotDefender>();
        navigation .ChaseTarget<FirstPersonMovement>();
    }
}
