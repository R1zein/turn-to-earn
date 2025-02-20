using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NPCNavigation : MonoBehaviour
{
    private Dictionary<Transform, float> priorityTargets = new ();
    public float sightDistance;
    public float attackDistance;
    private NavMeshAgent agent;
    [HideInInspector]public Transform target;
    private Animator animator;
    private float timer;
    public bool isDead;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
    public void FirstLook<T>(int priority) where T: MonoBehaviour
    {
        T[] ObjectsT = FindObjectsByType<T>(FindObjectsSortMode.None);
        Transform newTarget = null;
        float score = 0;
        foreach (T objectT in ObjectsT)
        {
            float currentDistance = Vector3.Distance(transform.position, objectT.transform.position);
            float currScore = priority / currentDistance;
            if (currScore > score)
            {
                newTarget = objectT.transform;
                score = currScore;
            }

        }
        if (newTarget != null)
        {
            priorityTargets.Add(newTarget, score);
        }



    }
    public void ChaseTarget<T>(int priority) where T : MonoBehaviour
    {
        timer += Time.deltaTime;
        if (timer > 1f)
        {
            timer = 0f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, sightDistance);
            Transform newTarget = null;
            float score = 0;
            foreach (Collider collider in colliders)
            {
                if (collider.TryGetComponent<T>(out var bot))
                {
                    float distance = Vector3.Distance(transform.position, bot.transform.position);
                    float currentScore = priority / distance;
                    if (currentScore > score)
                    {
                        score = currentScore;
                        newTarget = bot.transform;
                    }
                }
            }

            if (newTarget != null)
            {
                priorityTargets.Add(newTarget, score);
            }
        }
    }


    public void SetAndRefresh()
    {
        float currentScore = 0;
        foreach (var target in priorityTargets)
        {
            if (target.Value > currentScore)
            {
                currentScore = target.Value;
            }
        }
        var newTarget = priorityTargets.FirstOrDefault(x => x.Value.Equals(currentScore)).Key;
        target = newTarget;
        priorityTargets.Clear();
    }


}
