using UnityEngine;
using Zenject;

// Drops a bomb from under the drone on Bomb (F), at most once per
// GameConfig.BombCooldown. Called from DroneControl.Update, so all drone input
// lives in one place.
[RequireComponent(typeof(Rigidbody))]
public class DroneBombBay : MonoBehaviour
{
    [Inject] private InputService input;
    [Inject] private GameConfig config;
    [Inject] private BombSpawner spawner;

    [Tooltip("Where the bomb appears, in the drone's own frame: under the belly")]
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, -1.6f, 0.2f);

    private Rigidbody body;
    private Collider[] ownColliders;
    private float readyAt;

    // Seconds until the next bomb; 0 = ready.
    public float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();
    }

    public void HandleInput()
    {
        if (input.DroneBombDropped && CooldownLeft <= 0f)
            Drop();
    }

    private void Drop()
    {
        readyAt = Time.time + config.BombCooldown;

        Vector3 velocity = body.linearVelocity;
        Quaternion facing = Quaternion.LookRotation(velocity.sqrMagnitude > 1f ? velocity : Vector3.down, transform.up);
        DroneBomb bomb = spawner.Spawn(config.BombPrefab, transform.TransformPoint(dropOffset), facing);
        bomb.Launch(velocity, ownColliders);
    }
}
