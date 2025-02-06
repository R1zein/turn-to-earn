using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public abstract class NPCFacade : MonoBehaviour
{
    protected Animator animator;
    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected Collider _collider;
    protected NPCNavigation navigation;
    protected StatsHandler statsHandler;
    public AllResources requiredResources;
    public OnNpcDeath onNpcDeath;

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
    }
    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
    }
    protected void Update()
    {
        Navigation();
        NPCAnimationControl();
    }
    private void Death()
    {
        animator.SetBool("Death", true);
        _collider.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        agent.enabled = false;
        if(navigation != null)
        {
            navigation.enabled = false;
        }
        onNpcDeath.SendEventMessage();
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
