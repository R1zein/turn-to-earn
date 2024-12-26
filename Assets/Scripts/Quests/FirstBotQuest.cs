using UnityEngine;
[CreateAssetMenu(fileName = "FirstBotCreated", menuName = "Quest/FirstBotCreated")]
public class FirstBotQuest : Quest
{
    public bool botCreated;
    public override bool IsQuestComplited()
    {
        return botCreated;
    }
}
