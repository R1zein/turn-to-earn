using UnityEngine;
using UnityEngine.AI;

public abstract class NPSFasade : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider _collider;
    private NPSNavigation enemyNavigation;
    private StatsHandler statsHandler;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        enemyNavigation = GetComponent<NPSNavigation>();
        statsHandler = GetComponent<StatsHandler>();
    }
    private void Death()
    {
        animator.SetBool("Death", true);
        _collider.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        agent.enabled = false;
        enemyNavigation.enabled = false;
        Destroy(gameObject, 3);
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
