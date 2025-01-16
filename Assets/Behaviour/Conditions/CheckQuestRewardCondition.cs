using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckQuestReward", story: "check if [quest] reward gained", category: "Conditions", id: "22b5a78887866254d7377f63defb2303")]
public partial class CheckQuestRewardCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;

    public override bool IsTrue()
    {
        return Quest.Value.rewardGained;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
