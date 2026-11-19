using UnityEngine;
using Zenject;

// Body side of building: tells BuildService where the player is aiming, turns the
// ghost with the wheel, lifts it and confirms placement. While dismantling, the same
// aim and confirm go to Dismantler. The menu is BuildPanelController; where the ghost
// actually goes is BuildService's rule.
public class Inventory : MonoBehaviour
{
    [Inject] private BuildService buildService;
    [Inject] private Dismantler dismantler;
    [Inject] private InputService input;
    [Inject] private Camera aimCamera;

    public float buildDistance;
    public float scrollSpeed;

    private void OnEnable()
    {
        input.OnBuildConfirm += Confirm;
        input.OnBuildRaise += Raise;
        input.OnBuildLower += Lower;
    }

    private void OnDisable()
    {
        input.OnBuildConfirm -= Confirm;
        input.OnBuildRaise -= Raise;
        input.OnBuildLower -= Lower;
    }

    private void Update()
    {
        if (buildService.Current == null && !dismantler.IsActive)
            return;

        buildService.Turn(input.BuildRotate, scrollSpeed);

        // Triggers are not ground: a turret's 15 m range sphere or the sea's hull would
        // catch the ray long before it reaches the terrain.
        RaycastHit hit;
        Ray screenRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        bool isHit = Physics.Raycast(screenRay, out hit, buildDistance, Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Viewmodel"), QueryTriggerInteraction.Ignore);
        if (dismantler.IsActive)
            dismantler.Aim(isHit ? hit.collider : null);
        else if (isHit)
            buildService.Aim(hit.point);
    }

    private void Confirm()
    {
        if (dismantler.IsActive)
            dismantler.TryDismantle();
        else
            buildService.TryPlaceCurrent();
    }

    private void Raise() => buildService.ChangeHeight(1);

    private void Lower() => buildService.ChangeHeight(-1);
}
