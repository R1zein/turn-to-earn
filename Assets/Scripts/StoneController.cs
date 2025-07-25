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
            StoredResources.instance.AddResources(new AllResources(0, 0, oneHitResource, 0));
            if (resourceStore <= 0)
            {
                Death();
            }
        }
    }
}                                                              
                                                               