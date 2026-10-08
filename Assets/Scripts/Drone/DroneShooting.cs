using UnityEngine;
using Zenject;

public class DroneShooting : MonoBehaviour
{
    [Inject] private InputService input;

    public Transform spr;
    public Transform spl;
    [Tooltip("Seconds between damage ticks while firing")]
    public float interval = 0.1f;
    [Tooltip("Damage dealt to a zombie on each tick")]
    public float damage;
    public HovlLaser laserLeft;
    public HovlLaser laserRight;
    [Tooltip("Log/draw raycasts to diagnose hits in the editor")]
    public bool debug;

    private bool firing;
    private float damageTimer;

    // Call this from DroneControl.Update() so all drone control stays centralized there.
    public void Shoot()
    {
        if (input.FireStarted)
        {
            firing = true;
            damageTimer = interval; // deal the first tick immediately
            laserLeft.EnablePrepare();
            laserRight.EnablePrepare();
        }

        if (input.FireEnded)
        {
            firing = false;
            laserLeft.DisablePrepare();
            laserRight.DisablePrepare();
        }

        if (!firing)
            return;

        damageTimer += Time.deltaTime;
        if (damageTimer >= interval)
        {
            damageTimer = 0f;
            TryDamage(laserLeft);
            TryDamage(laserRight);
        }
    }

    // Raycast along the same origin/direction the laser draws its beam,
    // so the damage lands exactly where the beam is visually hitting.
    private void TryDamage(HovlLaser laser)
    {
        if (laser == null)
            return;

        Vector3 origin = laser.transform.position;
        Vector3 direction = laser.transform.forward;

        if (debug)
            Debug.DrawRay(origin, direction * laser.MaxLength, Color.red);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, laser.MaxLength))
            return;

        if (debug)
            Debug.Log($"Laser hit: {hit.collider.name}", hit.collider);

        // StatsHandler may live on a parent of the collider the ray actually hit,
        // so search up the hierarchy instead of only the hit collider itself.
        // Friend or foe is decided by fraction, as for every other weapon.
        var stats = hit.collider.GetComponentInParent<StatsHandler>();
        if (stats != null && stats.fraction == Fraction.Enemy)
            stats.TakeDamage(damage);
    }
}
