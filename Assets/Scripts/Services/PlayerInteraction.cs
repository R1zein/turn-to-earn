using System;
using UnityEngine;
using Zenject;

// On the interact key (E), casts a ray from the screen centre and opens the window
// of the structure it hits, if that structure has any tabs. Knows neither tables
// nor walls: what a structure offers is the tabs' business.
public class PlayerInteraction : IInitializable, IDisposable
{
    // Also how near the structure panel shows the interact hint.
    public const float Reach = 2.5f;

    [Inject] private InputService input;
    [Inject] private Camera camera;
    [Inject] private StructureLookup structures;
    [Inject] private StructureWindowController window;

    public void Initialize() => input.OnInteract += Interact;

    public void Dispose() => input.OnInteract -= Interact;

    private void Interact()
    {
        // The camera lives on the player and is destroyed with it on death.
        if (!camera)
            return;

        Ray ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (!Physics.Raycast(ray, out RaycastHit hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Structure structure = structures.At(hit);
        if (structure != null)
            window.Open(structure);
    }
}
