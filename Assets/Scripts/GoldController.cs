using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldController : ResourceController
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
            wallet.Add(new AllResources(0, 0, 0, oneHitResource));
            if (resourceStore <= 0)
            {
                Death();
            }
        }
    }
}