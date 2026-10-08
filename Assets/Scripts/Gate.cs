using UnityEngine;
using Zenject;

public class Gate : MonoBehaviour
{
    private bool withInZone;
    private bool isGateOpen = false;
    public Animation animation;

    [Inject] private InputService input;

    private void OnEnable()
    {
        input.OnInteract += Toggle;
    }
    private void OnDisable()
    {
        input.OnInteract -= Toggle;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            withInZone = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            withInZone = false;
        }
    }

    private void Toggle()
    {
        if (!withInZone || animation.isPlaying)
            return;

        if (isGateOpen == false)
        {
            animation.Play("GateOpen");
            isGateOpen = true;
        }
        else
        {
            animation.Play("GateClose");
            isGateOpen = false;
        }
    }
}
