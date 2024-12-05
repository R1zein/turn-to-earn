using UnityEngine;
[CreateAssetMenu(fileName = "FirstBotCreated", menuName = "Quest/FirstBotCreated")]
public class FirstBotQuest : Quest
{
    public override bool IsQuestComplited()
    {
        return GameManager.instance.firstBotCreated == true;
    }
}
