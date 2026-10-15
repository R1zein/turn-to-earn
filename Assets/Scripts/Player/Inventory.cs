using UnityEngine;
using Zenject;

// Body side of building: carries the picked ghost in front of the player and
// confirms placement. The menu is BuildPanelController, the rules BuildService.
public class Inventory : MonoBehaviour
{
    [Inject] private BuildService buildService;
    [Inject] private InputService input;

    public float buildDistance;
    public float scrollSpeed;

    private void OnEnable()
    {
        input.OnBuildConfirm += Place;
    }

    private void OnDisable()
    {
        input.OnBuildConfirm -= Place;
    }

    private void Update()
    {
        Ghost ghost = buildService.Current;
        if (ghost == null)
            return;

        RaycastHit hit;
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(screenRay, out hit, buildDistance))
        {
            ghost.transform.position = hit.point;
        }
        float mouseScroll = input.BuildRotate * scrollSpeed;
        ghost.transform.Rotate(0, mouseScroll, 0);
    }

    private void Place()
    {
        buildService.TryPlaceCurrent();
    }
}
