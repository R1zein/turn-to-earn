using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{

    public GameObject purchasePanel;
    public GameObject upgradePanel;
    public void TryToSetActive()
    {
        if (!purchasePanel.activeSelf & !upgradePanel.activeSelf)
        {
            purchasePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void CloseShop()
    {
        purchasePanel.SetActive(false);
        upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
