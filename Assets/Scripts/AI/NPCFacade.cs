using UnityEngine;
using Zenject;

[RequireComponent(typeof(NPCMotor), typeof(TargetSeeker), typeof(StatsHandler))]
public abstract class NPCFacade : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attackDistance = 2f;
    [Inject] private WeaponSight sight;
    public AllResources requiredResources;

    protected Animator animator;
    protected NPCMotor motor;
    protected StatsHandler statsHandler;
    private Collider bodyCollider;
    private TargetSeeker seeker;
    private bool isHeldStill;

    public StatsHandler Health => statsHandler;
    public Vector3 AimPoint => bodyCollider.bounds.center;
    protected Targetable Target { get; private set; }
    protected virtual float EngageDistance => attackDistance;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        motor = GetComponent<NPCMotor>();
        bodyCollider = GetComponent<Collider>();
        seeker = GetComponent<TargetSeeker>();
        statsHandler = GetComponent<StatsHandler>();
    }

    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
        seeker.OnTargetChanged += OnTargetChanged;
        Target = seeker.Current;
    }

    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
        seeker.OnTargetChanged -= OnTargetChanged;
        Target = null;
    }

    private void OnTargetChanged(Targetable target) => Target = target;

    private void Update()
    {
        if (isHeldStill || statsHandler.IsDead)
            return;

        bool hasPortalOrder = motor.FollowPortalRoute();
        Vector3 point = default;
        bool engaging = !hasPortalOrder && CanEngage(out point);
        if (engaging)
        {
            motor.Stop();
            motor.Face(point);
        }
        else if (!hasPortalOrder)
        {
            if (Target != null && Target.IsAvailable)
                motor.MoveTo(Target.ClosestPoint(transform.position));
            else
                motor.Stop();
        }
        NPCAnimationControl(engaging);
    }

    private bool CanEngage(out Vector3 point)
    {
        point = Vector3.zero;
        if (Target == null || !Target.IsAvailable || motor.IsAirborne || motor.IsTraversingLink)
            return false;

        Vector3 origin = AimPoint;
        point = Target.ClosestPoint(origin);
        Vector3 direction = point - origin;
        if (direction.sqrMagnitude > EngageDistance * EngageDistance)
            return false;
        return direction.sqrMagnitude < 0.0001f
            || sight.Trace(transform, new Ray(origin, direction.normalized), direction.magnitude) is not RaycastHit hit
            || hit.transform.IsChildOf(Target.transform);
    }

    // A shooting-range dummy can settle vertically and still takes damage.
    public void HoldStill()
    {
        isHeldStill = true;
        seeker.enabled = false;
        motor.Park(RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);
        animator.SetBool("Idle", true);
    }

    private void Death()
    {
        seeker.enabled = false;
        animator.SetBool("Death", true);
        bodyCollider.enabled = false;
        motor.Park(RigidbodyConstraints.FreezeAll);
        Destroy(gameObject, 3f);
    }

    protected virtual void NPCAnimationControl(bool engaging)
    {
        animator.SetBool("Attack", engaging);
        animator.SetBool("Run", !engaging && motor.IsMoving);
        animator.SetBool("Idle", !engaging && !motor.IsMoving);
    }
}
