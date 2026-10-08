using UnityEngine;
using Zenject;

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
        input.OnToolNext += Next;
        input.OnToolPrevious += Previous;
    }

    private void OnDisable()
    {
        input.OnToolNext -= Next;
        input.OnToolPrevious -= Previous;
    }

    private void Next()
    {
        toolIndex++;
        if (toolIndex >= tools.Length)
        {
            toolIndex = 0;
        }
        Swap();
    }

    private void Previous()
    {
        toolIndex--;
        if (toolIndex < 0)
        {
            toolIndex = tools.Length - 1;
        }
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
