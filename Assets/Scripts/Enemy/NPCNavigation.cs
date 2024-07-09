using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCNavigation : MonoBehaviour
{
    public float sightDistance;
    public float attackDistance;
    private NavMeshAgent agent;
    private Transform target;
    private Animator animator;
    private float timer;
    public bool isDead;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }
    public void FirstLook<T>() where T: MonoBehaviour
    {
        T[] bots = FindObjectsByType<T>(FindObjectsSortMode.None);
        float distance = Mathf.Infinity;
        foreach (T bot in bots)
        {
            float currentDistance = Vector3.Distance(transform.position, bot.transform.position);
            if (currentDistance < distance)
            {
                target = bot.transform;
                distance = currentDistance;
            }

        }
        if (target == null)
            return;
        agent.SetDestination(target.transform.position);
    }
    public void ChaseTarget<T>() where T : MonoBehaviour
    {
        NPCAnimationControl();

        timer += Time.deltaTime;
        if (timer > 1f)
        {
            timer = 0f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, sightDistance);
            float shortestDistance = Mathf.Infinity;
            foreach (Collider collider in colliders)
            {
                if (collider.TryGetComponent<T>(out var bot))
                {
                    float distance = Vector3.Distance(transform.position, bot.transform.position);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        target = bot.transform;
                    }
                }
            }
            if (target != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(target.transform.position);
            }
        }
    }

    private void NPCAnimationControl()
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
