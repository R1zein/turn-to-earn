using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{

    public GameObject purchasePanel;
    public GameObject upgradePanel;
    public AllResources neadbleResources;
    public GameObject bot;
    public Transform botSpawnPosition;
    public void TryToSetActive()
    {
        if (!purchasePanel.activeSelf & !upgradePanel.activeSelf)
        {
            purchasePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void CloseShop()
    {
        purchasePanel.SetActive(false);
        upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ByeBot()
    {
        if(neadbleResources.CompareTo(StoredResources.instance.GetResources()) >= 1)
        {
            Instantiate(bot, botSpawnPosition.position, Quaternion.identity);
        }
    }
}
[Serializable]
public class AllResources: IComparable<AllResources>
{
    public int iron;
    public int tree;
    public int ore;

    public int CompareTo(AllResources other)
    {
        if(iron == other.iron && tree == other.tree && ore == other.ore)
        {
            return 0;
        }
        else if(iron > other.iron && tree > other.tree && ore > other.ore)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }
}
