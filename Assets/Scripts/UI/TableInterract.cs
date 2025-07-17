using UnityEngine;

public class TableInterract : Interactable
{
    public Transform spawnPosition;

    private ShopController shopController;

    private void Start()
    {
        shopController = FindAnyObjectByType<ShopController>();
    }
    public override void Interract()
    {
        shopController.TryToSetActive(spawnPosition.position);
    }
}
