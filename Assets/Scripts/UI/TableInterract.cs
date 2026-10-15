using UnityEngine;
using Zenject;

public class TableInterract : Interactable
{
    [Inject] private ShopPanelController shopPanel;

    public Transform spawnPosition;

    public override void Interract()
    {
        shopPanel.Show(spawnPosition.position);
    }
}
