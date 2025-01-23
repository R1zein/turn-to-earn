using System;
using Unity.Behavior.GraphFramework;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/OnBotCreated")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "OnBotCreated", message: "when bot created", category: "Events", id: "160249bfb7999d0de98da0cb94b89f36")]
public partial class OnBotCreated : EventChannelBase
{
    public delegate void OnBotCreatedEventHandler();
    public event OnBotCreatedEventHandler Event;
    public bool firstBotCrea6ted;


    public void SendEventMessage()
    {
        Event?.Invoke();
    }

    public override void SendEventMessage(BlackboardVariable[] messageData)
    {
        Event?.Invoke();
    }

    public override Delegate CreateEventHandler(BlackboardVariable[] vars, System.Action callback)
    {
        OnBotCreatedEventHandler del = () =>
        {
            callback();
        };
        return del;
    }

    public override void RegisterListener(Delegate del)
    {
        Event += del as OnBotCreatedEventHandler;
    }

    public override void UnregisterListener(Delegate del)
    {
        Event -= del as OnBotCreatedEventHandler;
    }
}

