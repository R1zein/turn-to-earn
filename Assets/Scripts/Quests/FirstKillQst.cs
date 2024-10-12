using UnityEngine;
[CreateAssetMenu(fileName = "FirstKillQuest", menuName = "Quest/FirstKillQuest")]
public class FirstKillQst : Quest
{
    public override bool IsQuestComplited()
    {
        return GameManager.instance.firstKillMaded == true;
    }

}
