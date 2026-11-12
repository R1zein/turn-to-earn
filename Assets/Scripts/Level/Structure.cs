using System;
using System.Collections.Generic;
using UnityEngine;

// What a structure can turn into in its own place (StructureUpgrades): a wall into a
// wall with a ladder, later a wooden wall into a stone one. The card in the MODIFY tab
// shows the result's icon and name.
[Serializable]
public class StructureUpgrade
{
    [Tooltip("What it turns into. The first of these that fits on the grid is built: the same result placed differently, like a ladder at either end of a wall")]
    [SerializeField] private Structure[] results = new Structure[0];
    [SerializeField] private AllResources price = new AllResources();
    [Tooltip("One line under the cards about what the upgrade gives")]
    [SerializeField] private string description;

    public IReadOnlyList<Structure> Results => results;
    public Structure Result => results[0];
    public AllResources Price => price;
    public string Description => description;
}

// Anything the player builds: a wall, a tower, a gate, a turret. Has a name, an
// icon and health, which the structure panel shows while it is aimed at, and can
// be repaired with a hammer. At 0 HP its own body (Building, GatlingGun) destroys it.
// What it can be upgraded to is listed here, on the prefab, like its name.
[RequireComponent(typeof(StatsHandler))]
public class Structure : MonoBehaviour
{
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;
    [SerializeField] private StructureUpgrade[] upgrades = new StructureUpgrade[0];

    private StatsHandler stats;

    public string Title => title;
    public Sprite Icon => icon;
    public StatsHandler Stats => stats;
    public IReadOnlyList<StructureUpgrade> Upgrades => upgrades;

    private void Awake()
    {
        stats = GetComponent<StatsHandler>();
    }

    public void Repair(float amount) => stats.Heal(amount);
}
