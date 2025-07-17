using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    public event Action<bool> OnPanelStateChange;
    public GameObject purchasePanel;
    public GameObject upgradePanel;
    
    private Vector3 botSpawnPosition;
    public OnBotCreated onBotCreated;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void TryToSetActive(Vector3 spawnPosition)
    {
        botSpawnPosition = spawnPosition;
        if (!purchasePanel.activeSelf & !upgradePanel.activeSelf)
        {
            purchasePanel.SetActive(true);
            OnPanelStateChange?.Invoke(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseShop()
    {
        purchasePanel.SetActive(false);
        upgradePanel.SetActive(false);
        OnPanelStateChange?.Invoke(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ByeBot(NPCFacade bot)
    {
        if(StoredResources.instance.CurrentResources >= bot.requiredResources)
        {
            StoredResources.instance.DecreaseResources(bot.requiredResources);
            for (int i = 0; i < 1; i++)
            {
                Instantiate(bot, botSpawnPosition, Quaternion.identity);
                onBotCreated.SendEventMessage();
                onBotCreated.firstBotCrea6ted = true;
            }
        }
    }
}

