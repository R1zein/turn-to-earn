using UnityEngine;
using Zenject;

// While a panel holds the mouse, InputService switches the on-foot action map off,
// so walking and looking stop without this body having to know about panels.
public class Player : MonoBehaviour
{
    [Inject] private TargetRegistry registry;

    private StatsHandler statsHandler;
    public Camera deathCamera;

    void Awake()
    {
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
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
        registry.Add(TargetKind.Player, this);
    }

    private void OnDisable()
    {
        registry.Remove(TargetKind.Player, this);
        statsHandler.OnDeath -= Death;
    }
}
