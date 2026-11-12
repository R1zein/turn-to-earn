using UnityEngine;
using Zenject;

// The tool in hand: the number keys pick from `tools` in order, 1 is the first.
// A key past the end of the list does nothing.
public class ToolsSwap : MonoBehaviour
{
    public GameObject[] tools;
    private int toolIndex;

    [Inject] private InputService input;

    void Start()
    {
        Swap();
    }

    private void OnEnable()
    {
        input.OnToolSlot += Select;
    }

    private void OnDisable()
    {
        input.OnToolSlot -= Select;
    }

    private void Select(int slot)
    {
        if (slot >= tools.Length)
            return;

        toolIndex = slot;
        Swap();
    }

    private void Swap()
    {
        foreach (var tool in tools)
        {
            tool.SetActive(false);
        }
        tools[toolIndex].SetActive(true);
    }
}
