using System;

// The game's currency: four resource counts. Comparison operators hold only when
// they hold for ALL four components, so `wallet >= price` reads "can afford".
// It is a partial order: two amounts can be neither >= nor <= each other.
[Serializable]
public class AllResources : IEquatable<AllResources>
{
    public int iron;
    public int tree;
    public int ore;
    public int gold;

    public AllResources()
    {
    }

    public AllResources(int iron, int tree, int ore, int gold)
    {
        this.iron = iron;
        this.tree = tree;
        this.ore = ore;
        this.gold = gold;
    }

    public static bool operator >(AllResources left, AllResources right) =>
        left.iron > right.iron && left.tree > right.tree && left.ore > right.ore && left.gold > right.gold;

    public static bool operator <(AllResources left, AllResources right) =>
        left.iron < right.iron && left.tree < right.tree && left.ore < right.ore && left.gold < right.gold;

    public static bool operator >=(AllResources left, AllResources right) =>
        left.iron >= right.iron && left.tree >= right.tree && left.ore >= right.ore && left.gold >= right.gold;

    public static bool operator <=(AllResources left, AllResources right) =>
        left.iron <= right.iron && left.tree <= right.tree && left.ore <= right.ore && left.gold <= right.gold;

    public static bool operator ==(AllResources left, AllResources right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(AllResources left, AllResources right) => !(left == right);

    public static AllResources operator +(AllResources left, AllResources right) =>
        new AllResources(left.iron + right.iron, left.tree + right.tree, left.ore + right.ore, left.gold + right.gold);

    public static AllResources operator -(AllResources left, AllResources right) =>
        new AllResources(left.iron - right.iron, left.tree - right.tree, left.ore - right.ore, left.gold - right.gold);

    public bool Equals(AllResources other) =>
        !(other is null) && iron == other.iron && tree == other.tree && ore == other.ore && gold == other.gold;

    public override bool Equals(object obj) => Equals(obj as AllResources);

    public override int GetHashCode() => HashCode.Combine(iron, tree, ore, gold);

    public override string ToString() => $"iron {iron}, tree {tree}, ore {ore}, gold {gold}";
}
