using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronController : ResourceController
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
            Player.playerInstance.AddPlayerIron(oneHitResource);
        }
    }
}
