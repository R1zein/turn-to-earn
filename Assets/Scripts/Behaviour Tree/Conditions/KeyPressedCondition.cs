using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Key pressed", story: "Check if the [key] is pressed", category: "Conditions", id: "847843b87624d3ccdd24093f4c829d29")]
public partial class KeyPressedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<KeyCode> Key;
    public override bool IsTrue()
    {
        if(Input.GetKey(Key.Value))
        {
            Debug.Log("pressed");
        }
        else
        {
            Debug.Log("not pressed");
        }
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
