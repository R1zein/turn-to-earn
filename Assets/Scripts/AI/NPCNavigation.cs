using UnityEngine;
using UnityEngine.AI;

public class NPCNavigation : MonoBehaviour
{
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
    public void FirstLook<T>() where T: MonoBehaviour
    {
        T[] ObjectsT = FindObjectsByType<T>(FindObjectsSortMode.None);
        float distance = Mathf.Infinity;
        foreach (T objectT in ObjectsT)
        {
            float currentDistance = Vector3.Distance(transform.position, objectT.transform.position);
            if (currentDistance < distance)
            {
                target = objectT.transform;
                distance = currentDistance;
            }

        }
        if (target == null)
            return;
        agent.SetDestination(target.transform.position);
    }
    public void ChaseTarget<T>() where T : MonoBehaviour
    {
        

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

    
}
