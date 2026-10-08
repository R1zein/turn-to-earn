using UnityEngine;

public class Inventory : MonoBehaviour
{
    public GameObject towerPrefab;
    public Transform buildingPlant;

    public GameObject buildPanel;

    public float buildDistance;
    public float scrollSpeed;

    private Ghost ghost;


    private void Update()
    {
        if (ghost != null)
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                Instantiate(ghost.prefab, ghost.transform.position, ghost.transform.rotation);
                Destroy(ghost.gameObject);
            }
            RaycastHit hit;
            Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(screenRay, out hit, buildDistance))
            {
                ghost.transform.position = hit.point;
            }
            float mouseScroll = Input.mouseScrollDelta.y * scrollSpeed;
            ghost.transform.Rotate(0, mouseScroll, 0);
        }
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            buildPanel.SetActive(true);
        }
        


    }
    public void Build(Ghost ghostObject)
    {

        if (ghost == null)
        {
            if (StoredResources.instance.CurrentResources >= ghostObject.requiredResources)
            {
                StoredResources.instance.DecreaseResources(ghostObject.requiredResources);
                ghost = Instantiate(ghostObject);
            }               
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        buildPanel.SetActive(false);
    }

}

