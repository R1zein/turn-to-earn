using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasLastDialog", story: "check if the [quest] has last dialog", category: "Conditions", id: "1c855b3304d3db6365d134e22de204fa")]
public partial class HasLastDialogCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;

    public override bool IsTrue()
    {
        if (Quest.Value.currentIndex>Quest.Value.dialogData.CharacterSpeeches.Length)
        {
            return true;
        }
    return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
