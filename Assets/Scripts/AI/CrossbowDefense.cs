using UnityEngine;
using Zenject;

[RequireComponent(typeof(Animator), typeof(StatsHandler))]
public class CrossbowDefense : MonoBehaviour
{
    private const float ScanInterval = 0.25f;
    private const float TurnSpeed = 160f;
    private const float LaunchDelay = 0.07f;
    private const float ReloadTime = 2.8f;
    private const float Range = 35f;
    private const float Damage = 60f;
    private const float BoltSpeed = 39f;
    private static readonly int Fire = Animator.StringToHash("Fire");

    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject boltModel;

    [Inject] private TargetingService targeting;

    private Animator animator;
    private NPCFacade target;
    private float nextScan;
    private float nextShot;
    private float launchAt;
    private bool isFiring;

    private void Awake() => animator = GetComponent<Animator>();

    private void Update()
    {
        float now = Time.time;
        if (!isFiring && now >= nextScan)
        {
            target = targeting.FindVisibleEnemy(transform, muzzle.position, Range);
            nextScan = now + ScanInterval;
        }

        if (target != null)
        {
            Vector3 direction = target.AimPoint - muzzle.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(horizontal),
                    TurnSpeed * Time.deltaTime);

            if (!isFiring && now >= nextShot && Vector3.Angle(transform.forward, horizontal) < 4f
                && targeting.CanSeeEnemy(transform, muzzle.position, target, Range))
            {
                animator.SetTrigger(Fire);
                isFiring = true;
                launchAt = now + LaunchDelay;
                nextShot = now + ReloadTime;
            }
        }

        if (isFiring && now >= launchAt)
        {
            isFiring = false;
            if (targeting.CanSeeEnemy(transform, muzzle.position, target, Range))
                Launch(target.AimPoint - muzzle.position);
        }
    }

    private void Launch(Vector3 direction)
    {
        GameObject bolt = Instantiate(boltModel, muzzle.position, Quaternion.LookRotation(direction));
        bolt.AddComponent<CrossbowBolt>().Fly(targeting, transform, direction.normalized, BoltSpeed, Range, Damage);
    }
}
