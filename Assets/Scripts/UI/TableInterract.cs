using UnityEngine;
using Zenject;

public class TableInterract : Interactable
{
    [Inject] private ShopController shopController;

    public Transform spawnPosition;

    public override void Interract()
    {
        shopController.TryToSetActive(spawnPosition.position);
    }
}
