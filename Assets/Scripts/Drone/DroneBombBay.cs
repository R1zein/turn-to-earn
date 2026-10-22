using UnityEngine;
using Zenject;

// Drops a bomb from under the drone on Bomb (F), at most once per `cooldown`.
// The cooldown lives here, on the drone, so it is edited right in its inspector
// (asked for: "directly"); it is read live, so changing it in play mode also
// shortens or lengthens a cooldown already running. Called from
// DroneControl.Update, so all drone input lives in one place.
[RequireComponent(typeof(Rigidbody))]
public class DroneBombBay : MonoBehaviour
{
    [Inject] private InputService input;
    [Inject] private GameConfig config;
    [Inject] private BombSpawner spawner;

    [Tooltip("Seconds between bombs")]
    [Min(0f)]
    [SerializeField] private float cooldown = 30f;
    [Tooltip("Where the bomb appears, in the drone's own frame: under the belly")]
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, -1.6f, 0.2f);

    private Rigidbody body;
    private Collider[] ownColliders;
    private float droppedAt = float.NegativeInfinity;

    // Seconds until the next bomb; 0 = ready.
    public float CooldownLeft => Mathf.Max(0f, droppedAt + cooldown - Time.time);

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
        droppedAt = Time.time;

        Vector3 velocity = body.linearVelocity;
        Quaternion facing = Quaternion.LookRotation(velocity.sqrMagnitude > 1f ? velocity : Vector3.down, transform.up);
        DroneBomb bomb = spawner.Spawn(config.BombPrefab, transform.TransformPoint(dropOffset), facing);
        bomb.Launch(velocity, ownColliders);
    }
}
