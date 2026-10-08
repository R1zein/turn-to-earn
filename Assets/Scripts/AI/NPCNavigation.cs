using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

// Body side of choosing a target: holds the current target and walks the agent
// to it. Which target is best is TargetingService's decision.
public class NPCNavigation : MonoBehaviour
{
    private const float RescanInterval = 1f;

    [Inject] private TargetingService targeting;

    public float sightDistance;
    public float attackDistance;
    private NavMeshAgent agent;
    [HideInInspector]public Transform target;
    private float timer;
    public bool isDead;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    // Whole map, once when the NPC appears: find somewhere to go at all.
    public void LookEverywhere(IReadOnlyList<TargetWeight> weights)
    {
        Transform best = targeting.FindBest(transform.position, weights);
        if (best != null)
            target = best;
    }

    // Within sight, at most once per second, all kinds at once. Nothing in sight
    // keeps the current target.
    public void Rescan(IReadOnlyList<TargetWeight> weights)
    {
        timer += Time.deltaTime;
        if (timer < RescanInterval)
            return;
        timer = 0f;

        Transform best = targeting.FindBest(transform.position, weights, sightDistance);
        if (best != null)
            target = best;
    }
}
