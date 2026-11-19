using UnityEngine;
using Zenject;

[RequireComponent(typeof(TargetSeeker), typeof(StatsHandler))]
public class GatlingGun : MonoBehaviour
{
    [Inject] private WeaponSight sight;

    public Transform go_baseRotation;
    public Transform go_GunBody;
    public Transform go_barrel;
    public float barrelRotationSpeed;
    public float firingRange;
    public ParticleSystem muzzelFlash;
    public float fireRate = 0.1f;
    public float damage;

    private TargetSeeker seeker;
    private StatsHandler stats;
    private float spin;
    private float nextShot;

    private void Awake()
    {
        stats = GetComponent<StatsHandler>();
        seeker = GetComponent<TargetSeeker>();
        seeker.ConfigureSight(muzzelFlash.transform, CanSee);
        GetComponent<SphereCollider>().radius = seeker.Profile.MaxDistance > 0f ? seeker.Profile.MaxDistance : firingRange;
    }

    private void OnEnable() => stats.OnDeath += Death;

    private void OnDisable() => stats.OnDeath -= Death;

    private void Update()
    {
        Targetable target = seeker.Current;
        bool canFire = seeker.IsInRange(target) && CanSee(target);
        if (canFire)
        {
            Vector3 aim = target.AimPoint;
            Vector3 horizontal = Vector3.ProjectOnPlane(aim - go_baseRotation.position, Vector3.up);
            if (horizontal.sqrMagnitude > 0.001f)
                go_baseRotation.rotation = Quaternion.LookRotation(horizontal);
            if ((aim - go_GunBody.position).sqrMagnitude > 0.001f)
                go_GunBody.LookAt(aim);

            if (Time.time >= nextShot && CanSee(target))
            {
                nextShot = Time.time + fireRate;
                target.Health.TakeDamage(damage);
            }
        }

        spin = Mathf.MoveTowards(spin, canFire ? barrelRotationSpeed : 0f, barrelRotationSpeed * 6f * Time.deltaTime);
        go_barrel.Rotate(0f, 0f, spin * Time.deltaTime);
        if (canFire && !muzzelFlash.isPlaying)
            muzzelFlash.Play();
        else if (!canFire && muzzelFlash.isPlaying)
            muzzelFlash.Stop();
    }

    private bool CanSee(Targetable target) => sight.CanSee(transform, muzzelFlash.transform.position, target);

    private void OnDrawGizmosSelected()
    {
        float range = GetComponent<TargetSeeker>()?.Profile?.MaxDistance ?? 0f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range > 0f ? range : firingRange);
    }

    private void Death()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
