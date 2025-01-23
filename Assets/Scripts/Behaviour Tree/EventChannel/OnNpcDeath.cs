using System;
using Unity.Behavior.GraphFramework;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/OnNPCDeath")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "OnNPCDeath", message: "fire when NpC is dead", category: "Events", id: "b859eb023a38829e4378c9247d3e2ad6")]
public partial class OnNpcDeath : EventChannelBase
{
    public delegate void OnNpcDeathEventHandler();
    public event OnNpcDeathEventHandler Event;
    public int countNPC;

    public void SendEventMessage()
    {
        countNPC ++;
        Event?.Invoke();
    }

    public override void SendEventMessage(BlackboardVariable[] messageData)
    {
        Event?.Invoke();
    }

    public override Delegate CreateEventHandler(BlackboardVariable[] vars, System.Action callback)
    {
        OnNpcDeathEventHandler del = () =>
        {
            callback();
        };
        return del;
    }

    public override void RegisterListener(Delegate del)
    {
        Event += del as OnNpcDeathEventHandler;
    }

    public override void UnregisterListener(Delegate del)
    {
        Event -= del as OnNpcDeathEventHandler;
    }
}

