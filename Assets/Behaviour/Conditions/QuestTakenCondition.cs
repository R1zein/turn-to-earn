using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "QuestTaken", story: "Check if the [Quest] is taken", category: "Conditions", id: "107955d9c5f1edc3e3b4770cbddd6dd2")]
public partial class QuestTakenCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;

    public override bool IsTrue()
    {
        return Quest.Value.IsTaken;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
