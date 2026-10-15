using UnityEngine;
using Zenject;

// The point over an NPC or a turret where its health bar is drawn. Shows the
// health of the nearest StatsHandler up the hierarchy.
public class HealthBarAnchor : MonoBehaviour
{
    [Inject] private HealthBars bars;

    public StatsHandler Stats { get; private set; }

    private void Awake()
    {
        Stats = GetComponentInParent<StatsHandler>();
    }

    private void OnEnable()
    {
        bars.Add(this);
    }

    private void OnDisable()
    {
        bars.Remove(this);
    }
}
