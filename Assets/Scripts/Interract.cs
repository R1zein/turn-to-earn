using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interract : MonoBehaviour
{
    public KeyCode interractButton;

    private void Update()
    {
        if (Input.GetKeyDown(interractButton))
        {
            InterractWithObject();
        }
    }

    private void InterractWithObject()
    {
        RaycastHit hit;
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(screenRay, out hit, 1.5f))
        {
            if(hit.collider.gameObject.TryGetComponent<Interactable>(out var item))
            {
                item.Interract();
            }
        }
    }
}
