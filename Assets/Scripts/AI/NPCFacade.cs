using UnityEngine;
using Zenject;

public abstract class NPCFacade : MonoBehaviour
{
    [Inject] protected GameConfig config;
    [Inject] private TargetRegistry registry;

    protected Animator animator;
    protected NPCMotor motor;
    private Rigidbody rb;
    protected Collider _collider;
    protected NPCNavigation navigation;
    protected StatsHandler statsHandler;
    public AllResources requiredResources;
    private bool isHeldStill;

    public StatsHandler Health => statsHandler;
    public Vector3 AimPoint => _collider.bounds.center;

    // What others see this NPC as; null = nobody targets it.
    protected abstract TargetKind? Kind { get; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        motor = GetComponent<NPCMotor>();
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        navigation = GetComponent<NPCNavigation>();
        statsHandler = GetComponent<StatsHandler>();
    }
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
        if (Kind.HasValue)
            registry.Add(Kind.Value, this);
    }
    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
        if (Kind.HasValue)
            registry.Remove(Kind.Value, this);
    }
    protected virtual void Update()
    {
        if (isHeldStill || statsHandler.IsDead)
            return;

        Navigation();
        NPCAnimationControl();
    }

    // A shooting-range dummy: no target search, no walking, idle animation. It
    // still takes damage and dies as usual. The body may only settle vertically,
    // so it lands on the ground but cannot be shoved or toppled.
    public void HoldStill()
    {
        isHeldStill = true;
        motor.enabled = false;
        if (navigation != null)
            navigation.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation
            | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        animator.SetBool("Idle", true);
    }
    private void Death()
    {
        // The body lingers for the death animation; it is no longer a target.
        if (Kind.HasValue)
            registry.Remove(Kind.Value, this);

        animator.SetBool("Death", true);
        _collider.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        motor.enabled = false;
        if(navigation != null)
        {
            navigation.enabled = false;
        }
        Destroy(gameObject, 3);
    }
    protected abstract void Navigation();



    protected virtual void NPCAnimationControl()
    {
        bool attacking = navigation.InAttackRange(navigation.attackDistance);
        animator.SetBool("Attack", attacking);
        animator.SetBool("Run", !attacking && motor.IsMoving);
        animator.SetBool("Idle", !attacking && !motor.IsMoving);
        if (attacking)
        {
            motor.Stop();
            motor.Face(navigation.target.position);
        }
        else
            navigation.Follow();
    }
}
