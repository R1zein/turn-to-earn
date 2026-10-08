using UnityEngine;
using UnityEngine.AI;
using Zenject;

public abstract class NPCFacade : MonoBehaviour
{
    [Inject] protected GameConfig config;
    [Inject] private TargetRegistry registry;

    protected Animator animator;
    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected Collider _collider;
    protected NPCNavigation navigation;
    protected StatsHandler statsHandler;
    public AllResources requiredResources;

    // What others see this NPC as; null = nobody targets it.
    protected abstract TargetKind? Kind { get; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
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
    protected void Update()
    {
        Navigation();
        NPCAnimationControl();
    }
    private void Death()
    {
        // The body lingers for the death animation; it is no longer a target.
        if (Kind.HasValue)
            registry.Remove(Kind.Value, this);

        animator.SetBool("Death", true);
        _collider.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        agent.enabled = false;
        if(navigation != null)
        {
            navigation.enabled = false;
        }
        Destroy(gameObject, 3);
    }
    protected abstract void Navigation();



    protected virtual void NPCAnimationControl()
    {
        if (navigation.target == null)
        {
            animator.SetBool("Idle", true);
            return;
        }
        animator.SetBool("Run", true);
        if (Vector3.Distance(transform.position, navigation.target.transform.position) <= navigation.attackDistance)
        {
            animator.SetBool("Attack", true);
            animator.SetBool("Run", false);
            return;
        }
    }
}
