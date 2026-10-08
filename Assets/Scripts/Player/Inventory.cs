using UnityEngine;
using Zenject;

public class Inventory : MonoBehaviour
{
    [Inject] private ResourceWallet wallet;
    [Inject] private InputService input;
    // Buildings and their ghosts inject services (the shop table needs ShopController,
    // and the table ghost nests a Table), so both are born through the container.
    [Inject] private DiContainer container;

    public GameObject towerPrefab;
    public Transform buildingPlant;

    public GameObject buildPanel;

    public float buildDistance;
    public float scrollSpeed;

    private Ghost ghost;
    private bool isMenuOpen;

    private void OnEnable()
    {
        input.OnBuildMenu += OpenMenu;
        input.OnBuildConfirm += Place;
        input.OnUiClose += CloseMenu;
    }

    private void OnDisable()
    {
        input.OnBuildMenu -= OpenMenu;
        input.OnBuildConfirm -= Place;
        input.OnUiClose -= CloseMenu;
    }

    private void Update()
    {
        if (ghost != null)
        {
            RaycastHit hit;
            Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(screenRay, out hit, buildDistance))
            {
                ghost.transform.position = hit.point;
            }
            float mouseScroll = input.BuildRotate * scrollSpeed;
            ghost.transform.Rotate(0, mouseScroll, 0);
        }
    }

    private void Place()
    {
        if (ghost == null)
            return;

        container.InstantiatePrefab(ghost.prefab, ghost.transform.position, ghost.transform.rotation, null);
        Destroy(ghost.gameObject);
    }

    private void OpenMenu()
    {
        if (isMenuOpen)
            return;

        isMenuOpen = true;
        buildPanel.SetActive(true);
        input.PushMode(InputMode.Ui);
    }

    private void CloseMenu()
    {
        if (!isMenuOpen)
            return;

        isMenuOpen = false;
        buildPanel.SetActive(false);
        input.PopMode();
    }

    // Also called by the build buttons inside the shop's upgrade panel; then the
    // menu is not open and the shop keeps the cursor until it closes itself.
    public void Build(Ghost ghostObject)
    {

        if (ghost == null)
        {
            if (wallet.TrySpend(ghostObject.requiredResources))
            {
                ghost = container.InstantiatePrefabForComponent<Ghost>(ghostObject);
            }
        }

        CloseMenu();
    }

}
