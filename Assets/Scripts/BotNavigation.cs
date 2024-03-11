using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotNavigation : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private ResourceController currentResource;
    public float mineDistance;

    public float cooldown;
    private float timer;
    private Animator animator;
    public float startRadius;
    private float currentRadius;
    
    private IEnumerator BotMine()
    {
        while (true)
        {
            if (currentResource == null)
            {
                FindResource();
                yield return null;
            }
            yield return null;

        }
    }
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        StartCoroutine(BotMine());
    }
    private void FindResource()
    {
        if(startRadius <= 0)
        {
            startRadius = 1;
        }
        currentRadius = startRadius;
        bool flag = true;
        while(flag)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, currentRadius);
            foreach (Collider collider in colliders) 
            {
                if (collider.TryGetComponent<ResourceController>(out var resource))
                {
                    flag = false;
                    currentResource = resource;
                }
                
                
            }
            if(flag == true)
            {
                currentRadius *= 2;
            }

        }

        if (currentResource != null )
        {
            MoveToResource();
            animator.SetBool("Idle", false);
            animator.SetBool("Run", true);
        }
    }

    private void MoveToResource()
    {
        navMeshAgent.SetDestination(currentResource.transform.position);
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (currentResource != null)
        {
            float distance = Vector3.Distance(transform.position, currentResource.transform.position);
            if (distance <= mineDistance & timer>=cooldown)
            {
                timer = 0;
                BotMineResource();
                animator.SetBool("Run", false);
                animator.SetBool("Idle", true);
            }
        }
    }

    private void BotMineResource()
    {
        currentResource.TakeHit();
        animator.SetTrigger("Mine");
    }
}

