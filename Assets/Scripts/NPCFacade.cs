using UnityEngine;
using UnityEngine.AI;

public abstract class NPCFacade : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider _collider;
    private NPCNavigation enemyNavigation;
    private StatsHandler statsHandler;
    public AllResources requiredResources;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        enemyNavigation = GetComponent<NPCNavigation>();
        statsHandler = GetComponent<StatsHandler>();
    }
    private void Death()
    {
        animator.SetBool("Death", true);
        _collider.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        agent.enabled = false;
        if(enemyNavigation != null)
        {
            enemyNavigation.enabled = false;
        }

        Destroy(gameObject, 3);
        if(GameManager.instance.firstKillMaded == false ) 
        {
            GameManager.instance.firstKillMaded = true;
        }
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
    protected abstract void Navigation();
}
