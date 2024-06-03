using UnityEngine;
using UnityEngine.AI;

public class NPSFasade : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider _collider;
    private EnemyNavigation enemyNavigation;
    private StatsHandler statsHandler;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        enemyNavigation = GetComponent<EnemyNavigation>();
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
}
