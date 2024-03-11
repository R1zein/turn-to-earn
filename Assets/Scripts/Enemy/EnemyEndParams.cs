
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEndParams : StateMachineBehaviour
{
    public EnemyParams[] _params;
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (var param in _params)
        {
            animator.SetBool(param.animationName, param.value);
        }
    }
}
[Serializable]
public class EnemyParams
{
    public string animationName;
    public bool value;
}