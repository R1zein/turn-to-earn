using UnityEngine;
using Zenject;

public class GatlingGun : MonoBehaviour
{
    private const float ScanInterval = 0.25f;

    [Inject] private TargetRegistry registry;
    [Inject] private TargetingService targeting;

    public Transform go_baseRotation;
    public Transform go_GunBody;
    public Transform go_barrel;
    public float barrelRotationSpeed;
    public float firingRange;
    public ParticleSystem muzzelFlash;
    public float fireRate = 0.1f;
    public float damage;

    private NPCFacade target;
    private StatsHandler stats;
    private float spin;
    private float nextScan;
    private float nextShot;

    private void Awake()
    {
        GetComponent<SphereCollider>().radius = firingRange;
        stats = GetComponent<StatsHandler>();
    }

    private void OnEnable()
    {
        stats.OnDeath += Death;
        registry.Add(TargetKind.Turret, this);
    }

    private void OnDisable()
    {
        registry.Remove(TargetKind.Turret, this);
        stats.OnDeath -= Death;
    }

    private void Update()
    {
        Vector3 origin = muzzelFlash.transform.position;
        if (Time.time >= nextScan)
        {
            target = targeting.FindVisibleEnemy(transform, origin, firingRange);
            nextScan = Time.time + ScanInterval;
        }

        bool canFire = targeting.CanSeeEnemy(transform, origin, target, firingRange);
        if (canFire)
        {
            Vector3 aim = target.AimPoint;
            Vector3 horizontal = Vector3.ProjectOnPlane(aim - go_baseRotation.position, Vector3.up);
            if (horizontal.sqrMagnitude > 0.001f)
                go_baseRotation.rotation = Quaternion.LookRotation(horizontal);
            if ((aim - go_GunBody.position).sqrMagnitude > 0.001f)
                go_GunBody.LookAt(aim);

            if (Time.time >= nextShot && targeting.CanSeeEnemy(transform, muzzelFlash.transform.position, target, firingRange))
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, firingRange);
    }

    private void Death()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
