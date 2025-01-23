using System;
using System.Collections;
using Unity.AppUI.UI;
using UnityEngine;

public class BootStrap : MonoBehaviour
{
    async void Start()
    {
        await AsyncOperation();
        print("Hello World");
        await Awaitable.WaitForSecondsAsync(2f);
        print("End");
    }

    private async Awaitable AsyncOperation()
    {
        await Awaitable.WaitForSecondsAsync(5f);
        print("End of Method");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            print("Space");
        }
    }
}
