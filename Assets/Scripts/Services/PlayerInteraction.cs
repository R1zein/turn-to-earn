using System;
using UnityEngine;
using Zenject;

// On the interact key, casts a ray from the screen centre and uses whatever
// Interactable it hits. Knows neither tables nor any other concrete thing.
public class PlayerInteraction : IInitializable, IDisposable
{
    private const float Reach = 1.5f;

    [Inject] private InputService input;
    [Inject] private Camera camera;

    public void Initialize() => input.OnInteract += Interact;

    public void Dispose() => input.OnInteract -= Interact;

    private void Interact()
    {
        // The camera lives on the player and is destroyed with it on death.
        if (!camera)
            return;

        Ray ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, Reach)
            && hit.collider.gameObject.TryGetComponent<Interactable>(out var item))
        {
            item.Interract();
        }
    }
}
