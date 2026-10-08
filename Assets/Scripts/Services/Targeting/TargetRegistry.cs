using System.Collections.Generic;
using UnityEngine;

public enum TargetKind
{
    Player,
    BotMiner,
    BotDefender,
    Turret,
    Building,
    Enemy,
    ResourceNode,
}

// Everything NPCs and turrets may pick as a target. Bodies add themselves while
// enabled and alive; destroyed bodies are dropped on read (Unity-null), so the
// registry never hands out a dead object even if a removal was missed.
public class TargetRegistry
{
    private readonly Dictionary<TargetKind, HashSet<Component>> byKind =
        new Dictionary<TargetKind, HashSet<Component>>();

    public void Add(TargetKind kind, Component body) => Set(kind).Add(body);

    public void Remove(TargetKind kind, Component body) => Set(kind).Remove(body);

    public IReadOnlyCollection<Component> Get(TargetKind kind)
    {
        HashSet<Component> set = Set(kind);
        set.RemoveWhere(body => !body);
        return set;
    }

    private HashSet<Component> Set(TargetKind kind)
    {
        if (!byKind.TryGetValue(kind, out HashSet<Component> set))
        {
            set = new HashSet<Component>();
            byKind.Add(kind, set);
        }
        return set;
    }
}
