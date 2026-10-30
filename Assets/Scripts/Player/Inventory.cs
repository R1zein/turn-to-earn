using UnityEngine;
using Zenject;

// Body side of building: tells BuildService where the player is aiming and turns
// the ghost with the wheel, and confirms placement. The menu is
// BuildPanelController; where the ghost actually goes is BuildService's rule.
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
        if (buildService.Current == null)
            return;

        buildService.Turn(input.BuildRotate, scrollSpeed);

        // Triggers are not ground: a turret's 15 m range sphere or the sea's hull would
        // catch the ray long before it reaches the terrain.
        RaycastHit hit;
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(screenRay, out hit, buildDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            buildService.Aim(hit.point);
        }
    }

    private void Place()
    {
        buildService.TryPlaceCurrent();
    }
}
