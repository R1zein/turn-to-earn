using UnityEngine;

public class Gate : MonoBehaviour
{
    private bool withInZone;
    private bool isGateOpen = false;
    public Animation animation;

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
    private void Update()
    {
        if (withInZone)
        {
            if (Input.GetKeyDown(KeyCode.F) && animation.isPlaying == false)
            {
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
    }
}
