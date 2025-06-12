using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolsSwap : MonoBehaviour
{
    public GameObject[] tools;
    private int toolIndex;

    // Start is called before the first frame update
    void Start()
    {
        Swap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            toolIndex++;
            if(toolIndex >= tools.Length)
            {
                toolIndex = 0;
            }
            Swap();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            toolIndex--;
            if (toolIndex < 0)
            {
                toolIndex = tools.Length - 1;
            }
            Swap();
        }
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
