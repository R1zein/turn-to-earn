using UnityEngine;
using Zenject;

// The shop table: interacting with it opens the shop panel; bought bots appear at spawnPosition.
public class ShopTable : Interactable
{
    [Inject] private ShopPanelController shopPanel;

    public Transform spawnPosition;

    public override void Interact()
    {
        shopPanel.Show(spawnPosition.position);
    }
}
