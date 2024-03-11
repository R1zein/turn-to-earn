using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{

    public GameObject purchasePanel;
    public GameObject upgradePanel;
    public AllResources neadbleResources;
    public GameObject bot;
    public Transform botSpawnPosition;
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

    public void ByeBot()
    {
        if(StoredResources.instance.CurrentResources >= neadbleResources)
        {
            StoredResources.instance.DecreaseResources(neadbleResources);
            for (int i = 0; i < 1; i++)
            {
                Instantiate(bot, botSpawnPosition.position, Quaternion.identity);
            }
        }
    }
}

