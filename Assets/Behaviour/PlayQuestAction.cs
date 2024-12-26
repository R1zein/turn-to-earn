using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Play Quest", story: "Try To Play The [Quest]", category: "Action", id: "9a15d18eda36cffb1821cd60ad3390f2")]
public partial class PlayQuestAction : Action
{
    [SerializeReference] public BlackboardVariable<Quest> Quest;
    [SerializeReference] public BlackboardVariable<OnDialogButtonPressed> OnButtonPressed;
    private DialogManager dialogManager;
    
    protected override Status OnStart()
    {
        dialogManager = GameObject.FindAnyObjectByType<DialogManager>();
        Quest.Value.IsTaken = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        dialogManager.StartDialog(Quest.Value);
        return Status.Success;
    }
    

    protected override void OnEnd()
    {
        
    }
}

