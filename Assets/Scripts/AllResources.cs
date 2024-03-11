using System;

[Serializable]
public class AllResources
{
    public int iron;
    public int tree;
    public int ore;

    public AllResources()
    {

    }
    public AllResources(int iron, int tree, int ore)
    {
        this.iron = iron;
        this.tree = tree;
        this.ore = ore;
    }

    public static bool operator > (AllResources left, AllResources right)
    {
        if(left.iron > right.iron & left.tree > right.tree & left.ore > right.ore)
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
        if (left.iron < right.iron & left.tree < right.tree & left.ore < right.ore)
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
        if (left.iron == right.iron & left.tree == right.tree & left.ore == right.ore)
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
        if (left.iron != right.iron | left.tree != right.tree | left.ore != right.ore)
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
        if (left.iron >= right.iron & left.tree >= right.tree & left.ore >= right.ore)
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
        if (left.iron <= right.iron & left.tree <= right.tree & left.ore <= right.ore)
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
        return res;
    }
    public static AllResources operator - (AllResources left, AllResources right)
    {
        AllResources res = new AllResources();
        res.iron = left.iron - right.iron;
        res.tree = right.tree - right.tree;
        res.ore = left.ore - right.ore;
        return res;
    }
}

