using UnityEngine;

[CreateAssetMenu(fileName = "RecourcesQuest", menuName = "Quest/ResourcesQuest")]
public class ResourcesQuest : Quest
{
    public int stone;
    public int tree;
    public int iron;

    public override bool IsQuestComplited()
    {
        return StoredResources.instance.CurrentResources.ore >= stone &&
        StoredResources.instance.CurrentResources.tree >= tree &&
        StoredResources.instance.CurrentResources.iron >= iron;
    }
}

