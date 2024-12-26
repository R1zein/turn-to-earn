using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CreateBot", story: "create new [bot]", category: "Action", id: "a65c51a08325867f40bdbe379ee26529")]
public partial class CreateBotAction : Action
{
    [SerializeReference] public BlackboardVariable<BotDefender> Bot;

    protected override Status OnStart()
    {
        var table = GameObject.FindAnyObjectByType<TableInterract>();
        GameObject.Instantiate(Bot.Value, table.transform.position, Quaternion.identity);
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

