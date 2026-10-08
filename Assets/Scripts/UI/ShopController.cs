using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ShopController : MonoBehaviour
{
    [Inject] private ResourceWallet wallet;

    public event Action<bool> OnPanelStateChange;
    public GameObject purchasePanel;
    public GameObject upgradePanel;
    
    private Vector3 botSpawnPosition;
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
        if (wallet.TrySpend(bot.requiredResources))
        {
            Instantiate(bot, botSpawnPosition, Quaternion.identity);
        }
    }
}

