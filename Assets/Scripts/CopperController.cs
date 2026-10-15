using UnityEngine;

public class CopperController : ResourceController
{

    public override void TakeHit()
    {
        if (resourceStore <= 0)
        {
            Death();
        }
        else
        {
            resourceStore -= oneHitResource;
            wallet.Add(new AllResources(0, 0, 0, 0, oneHitResource));
            if (resourceStore <= 0)
            {
                Death();
            }
        }
    }
}
