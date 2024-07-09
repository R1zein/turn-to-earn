using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NPSFasade
{
    private NPSNavigation navigation;
    private void Start()
    {
        navigation = GetComponent<NPSNavigation>();
        navigation.FirstLook<BotNavigation>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotNavigation>();
    }
}
