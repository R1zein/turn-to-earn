using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RewardGained", story: "give reward for [quest]", category: "Action", id: "6fd19fb847677fb55e282e5f094dca50")]
public partial class RewardGainedAction : Action
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;

    protected override Status OnStart()
    {
        Quest.Value.rewardGained = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

