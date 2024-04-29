using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public GameObject towerPrefab;
    public Transform buildingPlant;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            Instantiate(towerPrefab, buildingPlant.position, Quaternion.identity);
        }
    }
}
