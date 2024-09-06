using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public GameObject towerPrefab;
    public Transform buildingPlant;

    public GameObject wallPrefab;
    public GameObject ghostWallPrefab;

    public float buildDistance;
    public float scrollSpeed;

    private GameObject ghost;

    private void Start()
    {
        
    }
    private void Update()
    {
        BuildTurrel();
        BuildWall();
        
        if(ghost != null)
        {
            RaycastHit hit;
            Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(screenRay, out hit, buildDistance))
            {
                ghost.transform.position = hit.point;
            }
            float mouseScroll = Input.mouseScrollDelta.y * scrollSpeed;
            ghost.transform.Rotate(0, mouseScroll, 0);
        }
    }
    private void BuildWall()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (ghost == null)
            {
                ghost = Instantiate(ghostWallPrefab);
                
            }
            else
            {
                Instantiate(wallPrefab, ghost.transform.position, ghost.transform.rotation);
                Destroy(ghost);
            }
            
        }
    }
    private void BuildTurrel()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Instantiate(towerPrefab, buildingPlant.position, Quaternion.identity);
        }
    }
    
}
