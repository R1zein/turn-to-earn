using UnityEngine;

// Sweep the actual flight segment so the bolt cannot tunnel through a target.
public class CrossbowBolt : MonoBehaviour
{
    private TargetingService targeting;
    private Transform owner;
    private Vector3 direction;
    private float speed;
    private float remaining;
    private float damage;

    public void Fly(TargetingService service, Transform shooter, Vector3 heading, float velocity, float range, float hitDamage)
    {
        targeting = service;
        owner = shooter;
        direction = heading;
        speed = velocity;
        remaining = range;
        damage = hitDamage;
    }

    private void FixedUpdate()
    {
        float distance = Mathf.Min(remaining, speed * Time.fixedDeltaTime);
        if (targeting.Trace(owner, new Ray(transform.position, direction), distance) is RaycastHit hit)
        {
            NPCFacade enemy = hit.collider.GetComponentInParent<NPCFacade>();
            if (enemy != null && enemy.Health.fraction == Fraction.Enemy)
                enemy.Health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;
        remaining -= distance;
        if (remaining <= 0f)
            Destroy(gameObject);
    }
}
