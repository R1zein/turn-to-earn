using System;
using UnityEngine;
using Zenject;

public class ShopController : MonoBehaviour
{
    [Inject] private ResourceWallet wallet;
    [Inject] private InputService input;

    public event Action<bool> OnPanelStateChange;
    public GameObject purchasePanel;
    public GameObject upgradePanel;

    private Vector3 botSpawnPosition;

    private bool IsOpen => purchasePanel.activeSelf || upgradePanel.activeSelf;

    private void OnEnable()
    {
        input.OnUiClose += CloseShop;
    }

    private void OnDisable()
    {
        input.OnUiClose -= CloseShop;
    }

    public void TryToSetActive(Vector3 spawnPosition)
    {
        botSpawnPosition = spawnPosition;
        if (!IsOpen)
        {
            purchasePanel.SetActive(true);
            OnPanelStateChange?.Invoke(false);
            input.PushMode(InputMode.Ui);
        }
    }

    public void CloseShop()
    {
        if (!IsOpen)
            return;

        purchasePanel.SetActive(false);
        upgradePanel.SetActive(false);
        OnPanelStateChange?.Invoke(true);
        input.PopMode();
    }

    public void ByeBot(NPCFacade bot)
    {
        if (wallet.TrySpend(bot.requiredResources))
        {
            Instantiate(bot, botSpawnPosition, Quaternion.identity);
        }
    }
}
