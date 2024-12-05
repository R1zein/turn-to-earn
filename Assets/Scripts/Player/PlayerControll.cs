using System;
using Unity.Behavior;
using UnityEngine;

public class PlayerControll : MonoBehaviour
{
    public OnDialogButtonPressed onDialogButtonPressed;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            onDialogButtonPressed.SendEventMessage();
        }
    }
}
