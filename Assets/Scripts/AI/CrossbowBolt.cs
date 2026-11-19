using UnityEngine;

// Sweep the actual flight segment so the bolt cannot tunnel through a target.
public class CrossbowBolt : MonoBehaviour
{
    private WeaponSight sight;
    private TargetProfile profile;
    private Transform owner;
    private Vector3 direction;
    private float speed;
    private float remaining;
    private float damage;

    public void Fly(WeaponSight raycasts, TargetProfile targetProfile, Transform shooter, Vector3 heading,
        float velocity, float range, float hitDamage)
    {
        sight = raycasts;
        profile = targetProfile;
        owner = shooter;
        direction = heading;
        speed = velocity;
        remaining = range;
        damage = hitDamage;
    }

    private void FixedUpdate()
    {
        float distance = Mathf.Min(remaining, speed * Time.fixedDeltaTime);
        if (sight.Trace(owner, new Ray(transform.position, direction), distance) is RaycastHit hit)
        {
            Targetable target = hit.collider.GetComponentInParent<Targetable>();
            if (target != null && target.IsAvailable && target.Health != null && profile.Targets(target.Faction))
                target.Health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;
        remaining -= distance;
        if (remaining <= 0f)
            Destroy(gameObject);
    }
}
