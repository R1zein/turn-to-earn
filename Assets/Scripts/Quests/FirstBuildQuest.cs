using UnityEngine;
[CreateAssetMenu(fileName = "FirstBuildQuest", menuName = "Quest/FirstBuildQuest")]
public class FirstBuildQuest : Quest
{
    public override bool IsQuestComplited()
    {
        return GameManager.instance.firstBuildCreated == true;
    }
}
