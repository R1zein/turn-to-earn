using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    public float sightDistance;
    public float attackDistance;
    private NavMeshAgent agent;
    private BotNavigation target;
    private Animator animator;
    private float timer;
    public bool isDead;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        BotNavigation[] bots = FindObjectsOfType<BotNavigation>();
        float distance = Mathf.Infinity;
        foreach (BotNavigation bot in bots)
        {
            float currentDistance = Vector3.Distance(transform.position, bot.transform.position);
            if (currentDistance < distance)
            {
                target = bot;
                distance = currentDistance;
            }

        }
        if (target == null)
            return;
        agent.SetDestination(target.transform.position);

    }


    private void Update()
    {
        EnemyAnimationControl();

        timer += Time.deltaTime;
        if (timer > 1f)
        {
            timer = 0f;
            ScanForTarget();
            if (target != null)
            {
                agent.SetDestination(target.transform.position);
            }
        }
    }

    private void ScanForTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sightDistance);
        float shortestDistance = Mathf.Infinity;
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<BotNavigation>(out var bot))
            {
                float distance = Vector3.Distance(transform.position, bot.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    target = bot;
                }
            }
        }
    }

    private void EnemyAnimationControl()
    {
        if (target == null)
        {
            animator.SetBool("Idle", true);
            return;
        }
        animator.SetBool("Run", true);
        if (Vector3.Distance(transform.position, target.transform.position) <= attackDistance)
        {
            animator.SetBool("Attack", true);
            animator.SetBool("Run", false);
            return;
        }
    }
}
