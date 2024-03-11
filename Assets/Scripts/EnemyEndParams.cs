
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEndParams : StateMachineBehaviour
{
    public EnemyParams param;
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(param.animationName, param.value);
    }
}
[Serializable]
public class EnemyParams
{
    public string animationName;
    public bool value;
}