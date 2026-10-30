using UnityEngine;

// A weapon trigger: hurts what it touches once on contact, if it belongs to the
// target fraction. Friend or foe is decided only by fraction. A building may be made
// of parts with their own colliders (the gate), so a hit on a part counts for it.
public class DamageDealer : MonoBehaviour
{
    [SerializeField] private Fraction targetFraction;
    [SerializeField] private float damage;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<StatsHandler>(out var stats))
        {
            Building building = other.GetComponentInParent<Building>();
            if (building == null)
                return;
            stats = building.GetComponent<StatsHandler>();
        }

        if (stats.fraction == targetFraction)
            stats.TakeDamage(damage);
    }
}
