using System;

[Serializable]
public class AllResources
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

    public static bool operator > (AllResources left, AllResources right)
    {
        if(left.iron > right.iron & left.tree > right.tree & left.ore > right.ore & left.gold > right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool operator < (AllResources left, AllResources right)
    {
        if (left.iron < right.iron & left.tree < right.tree & left.ore < right.ore & left.gold < right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool operator == (AllResources left, AllResources right)
    {
        if (left.iron == right.iron & left.tree == right.tree & left.ore == right.ore & left.gold == right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool operator != (AllResources left, AllResources right)
    {
        if (left.iron != right.iron | left.tree != right.tree | left.ore != right.ore & left.gold != right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool operator >= (AllResources left, AllResources right)
    {
        if (left.iron >= right.iron & left.tree >= right.tree & left.ore >= right.ore & left.gold >= right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool operator <= (AllResources left, AllResources right)
    {
        if (left.iron <= right.iron & left.tree <= right.tree & left.ore <= right.ore & left.gold <= right.gold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static AllResources operator + (AllResources left, AllResources right)
    {
        AllResources res = new AllResources();
        res.iron = left.iron + right.iron;
        res.tree = left.tree + right.tree;
        res.ore = left.ore + right.ore;
        res.gold = left.gold + right.gold;
        return res;
    }
    public static AllResources operator - (AllResources left, AllResources right)
    {
        AllResources res = new AllResources();
        res.iron = left.iron - right.iron;
        res.tree = left.tree - right.tree;
        res.ore = left.ore - right.ore;
        res.gold = left.gold - right.gold;
        return res;
    }
    

}

