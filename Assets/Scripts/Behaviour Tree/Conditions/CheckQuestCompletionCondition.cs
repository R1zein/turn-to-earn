using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Check Quest Completion", story: "Check if the [quest] is completed", category: "Conditions", id: "de71ce97c5911f75e1076c31390a92d5")]
public partial class CheckQuestCompletionCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;

    public override bool IsTrue()
    {
        return Quest.Value.IsQuestComplited();
    }

    public override void OnStart()
    {
        
    }

    public override void OnEnd()
    {
    }
}
