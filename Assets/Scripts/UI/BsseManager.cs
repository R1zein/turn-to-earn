using System;
using UnityEngine;

public class BsseManager : MonoBehaviour
{
    public GameObject fortButtons;
    public GameObject fortPartButtons;
    public GameObject BasePanel;
    public Fortification[] fortifications;
    [HideInInspector]public Fortification currentFortification;

    public void BuildFortification(Fortification buildFort)
    {
        if (currentFortification == null)
        {
            foreach (Fortification fort in fortifications)
            {
                if (fort.ID == buildFort.ID)
                {
                    currentFortification = fort;
                    fort.gameObject.SetActive(true);
                }
            }

        }
    }

    private void Update()
    {
        if (BasePanel.gameObject.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}