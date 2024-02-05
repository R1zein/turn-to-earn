using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class StoneController : ResourceController
{
    //проверяет остаток руды                                   
    public override void TakeHit()
    {
        if (resourceStore <= 0)
        {
            Death();
        }
        else
        {
            resourceStore -= oneHitResource;
            Player.playerInstance.AddPlayerOre(oneHitResource);
        }
    }
}                                                              
                                                               