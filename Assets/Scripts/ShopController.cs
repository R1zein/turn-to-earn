using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{

    public GameObject purchasePanel;
    public GameObject upgradePanel;
    
    public Transform botSpawnPosition;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void TryToSetActive()
    {
        if (!purchasePanel.activeSelf & !upgradePanel.activeSelf)
        {
            purchasePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }

    public void CloseShop()
    {
        purchasePanel.SetActive(false);
        upgradePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    public void ByeBot(NPCFacade bot)
    {
        if(StoredResources.instance.CurrentResources >= bot.requiredResources)
        {
            StoredResources.instance.DecreaseResources(bot.requiredResources);
            for (int i = 0; i < 1; i++)
            {
                Instantiate(bot, botSpawnPosition.position, Quaternion.identity);
            }
        }
    }
}

