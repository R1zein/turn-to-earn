using UnityEngine;
using Zenject;

// A dropped bomb: falls under gravity with the drone's speed, turns its nose
// along the fall, and blows up on the first thing it touches - ground, a zombie,
// a wall. What the blast does is the Explosions service's business.
[RequireComponent(typeof(Rigidbody))]
public class DroneBomb : MonoBehaviour
{
    // Dropped into a pit or off the map: blow up anyway rather than fall forever.
    private const float MaxLifetime = 20f;

    [Inject] private Explosions explosions;

    [Tooltip("1/s: how fast the nose turns into the direction of the fall")]
    [SerializeField] private float noseFollow = 5f;

    private Rigidbody body;
    private float bornAt;
    private bool hasExploded;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bornAt = Time.time;
    }

    // Leaves the bay with the drone's velocity, never touching the drone itself.
    public void Launch(Vector3 velocity, Collider[] dropper)
    {
        body.linearVelocity = velocity;
        foreach (Collider own in GetComponentsInChildren<Collider>())
            foreach (Collider other in dropper)
                Physics.IgnoreCollision(own, other);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        if (velocity.sqrMagnitude > 1f)
            body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(velocity),
                1f - Mathf.Exp(-noseFollow * Time.fixedDeltaTime)));

        if (Time.time - bornAt > MaxLifetime)
            Explode(body.position);
    }

    private void OnCollisionEnter(Collision collision) => Explode(collision.GetContact(0).point);

    private void Explode(Vector3 at)
    {
        if (hasExploded)
            return;
        hasExploded = true;
        explosions.Blast(at);
        Destroy(gameObject);
    }
}
