using UnityEngine;
using Zenject;

public class Player : MonoBehaviour
{
    [Inject] private ShopController shopController;

    private StatsHandler statsHandler;
    public Camera deathCamera;
    private FirstPersonLook firstPersonLook;
    private FirstPersonMovement firstPersonMovement;

    void Awake()
    {
        firstPersonLook = GetComponentInChildren<FirstPersonLook>();
        firstPersonMovement = GetComponent<FirstPersonMovement>();
        statsHandler = GetComponent<StatsHandler>();
    }
    private void Death()
    {
        var camera = Instantiate(deathCamera, transform.position, transform.rotation);
        var position = camera.transform.position;
        position.y += 5;
        position.z -= 2.5f;
        camera.transform.Rotate(70, 0, 0);
        camera.transform.position = position;
        Destroy(gameObject);
    }
    private void SetPlayerActive(bool active)
    {
        firstPersonLook.enabled = active;
        firstPersonMovement.enabled = active;
    }
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
        shopController.OnPanelStateChange += SetPlayerActive;
    }

    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
        shopController.OnPanelStateChange -= SetPlayerActive;
    }
}
