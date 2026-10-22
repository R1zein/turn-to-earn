using UnityEngine;
using Zenject;

// The drone's twin lasers. Both beams meet at one FOCUS point: a ray from midway
// between the two muzzles along the drone's nose finds what the nose is looking
// at (or the end of the beams' reach), and each laser is turned to it every
// frame - so the beams, and the damage rays along them, converge on the target.
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

    // Where both beams meet this frame.
    public Vector3 FocusPoint { get; private set; }

    // Call this from DroneControl.Update() so all drone control stays centralized there.
    public void Shoot()
    {
        AimAtFocus();

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

    // The focus is found from midway between the muzzles along the nose; the beams'
    // own reach caps it. Triggers (water, zones) are not targets.
    private void AimAtFocus()
    {
        Vector3 origin = (laserLeft.transform.position + laserRight.transform.position) * 0.5f;
        Vector3 nose = transform.forward;
        float reach = Mathf.Max(laserLeft.MaxLength, laserRight.MaxLength);
        FocusPoint = Physics.Raycast(origin, nose, out RaycastHit hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point
            : origin + nose * reach;

        Aim(laserLeft);
        Aim(laserRight);
    }

    private void Aim(HovlLaser laser)
    {
        Vector3 toFocus = FocusPoint - laser.transform.position;
        if (toFocus.sqrMagnitude > 0.01f)
            laser.transform.rotation = Quaternion.LookRotation(toFocus, transform.up);
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
