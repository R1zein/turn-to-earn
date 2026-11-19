using UnityEngine;
using Zenject;

[RequireComponent(typeof(Animator), typeof(StatsHandler), typeof(TargetSeeker))]
public class CrossbowDefense : MonoBehaviour
{
    private const float TurnSpeed = 160f;
    private const float LaunchDelay = 0.07f;
    private const float ReloadTime = 2.8f;
    private const float Damage = 60f;
    private const float BoltSpeed = 39f;
    private readonly int fire = Animator.StringToHash("Fire");

    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject boltModel;

    [Inject] private WeaponSight sight;

    private Animator animator;
    private TargetSeeker seeker;
    private Targetable pendingShot;
    private float nextShot;
    private float launchAt;
    private bool isFiring;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        seeker = GetComponent<TargetSeeker>();
        seeker.ConfigureSight(muzzle, CanSee);
    }

    private void OnDisable()
    {
        isFiring = false;
        pendingShot = null;
    }

    private void Update()
    {
        float now = Time.time;
        Targetable target = isFiring ? pendingShot : seeker.Current;
        if (target != null && target.IsAvailable)
        {
            Vector3 direction = target.AimPoint - muzzle.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(horizontal),
                    TurnSpeed * Time.deltaTime);

            if (!isFiring && now >= nextShot && Vector3.Angle(transform.forward, horizontal) < 4f
                && seeker.IsInRange(target) && CanSee(target))
            {
                animator.SetTrigger(fire);
                pendingShot = target;
                isFiring = true;
                launchAt = now + LaunchDelay;
                nextShot = now + ReloadTime;
            }
        }

        if (isFiring && now >= launchAt)
        {
            isFiring = false;
            if (seeker.IsInRange(pendingShot) && CanSee(pendingShot))
                Launch(pendingShot.AimPoint - muzzle.position);
            pendingShot = null;
        }
    }

    private bool CanSee(Targetable target) => sight.CanSee(transform, muzzle.position, target);

    private void Launch(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        float range = Mathf.Max(seeker.Profile.MaxDistance, direction.magnitude + 2f);
        GameObject bolt = Instantiate(boltModel, muzzle.position, Quaternion.LookRotation(direction));
        bolt.AddComponent<CrossbowBolt>().Fly(sight, seeker.Profile, transform, direction.normalized, BoltSpeed, range, Damage);
    }
}
