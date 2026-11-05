using UnityEngine;

// Anything the player builds: a wall, a tower, a gate, a turret. Has a name, an
// icon and health, which the structure panel shows while it is aimed at, and can
// be repaired with a hammer. At 0 HP its own body (Building, GatlingGun) destroys it.
[RequireComponent(typeof(StatsHandler))]
public class Structure : MonoBehaviour
{
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;

    private StatsHandler stats;

    public string Title => title;
    public Sprite Icon => icon;
    public StatsHandler Stats => stats;

    private void Awake()
    {
        stats = GetComponent<StatsHandler>();
    }

    public void Repair(float amount) => stats.Heal(amount);
}
