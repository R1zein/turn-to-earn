using UnityEngine;
using UnityEngine.AI;

public abstract class NPCFacade : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider _collider;
    protected NPCNavigation navigation;
    private StatsHandler statsHandler;
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
    private void Update()
    {
        Navigation();
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
}
