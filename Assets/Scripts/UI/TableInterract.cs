using UnityEngine;

public class TableInterract : Interactable
{
    public ShopController shopController;
    public override void Interract()
    {
        shopController.TryToSetActive();
    }
}
