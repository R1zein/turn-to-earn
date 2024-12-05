using System;
using Unity.Behavior.GraphFramework;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/OnDialogButtonPressed")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "OnDialogButtonPressed", message: "Happend When Dialog Button Pressed", category: "Events", id: "89f5c1a34994fdfab7d2b4373bb13b6b")]
public partial class OnDialogButtonPressed : EventChannelBase
{
    public delegate void OnDialogButtonPressedEventHandler();
    public event OnDialogButtonPressedEventHandler Event; 

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
        OnDialogButtonPressedEventHandler del = () =>
        {
            callback();
        };
        return del;
    }

    public override void RegisterListener(Delegate del)
    {
        Event += del as OnDialogButtonPressedEventHandler;
    }

    public override void UnregisterListener(Delegate del)
    {
        Event -= del as OnDialogButtonPressedEventHandler;
    }
}

