using UnityEngine;
[CreateAssetMenu(fileName = "FirstBotCreated", menuName = "Quest/FirstBotCreated")]
public class FirstBotQuest : Quest
{
    public OnBotCreated onBotCreated;
    public override bool IsQuestComplited()
    {
        return onBotCreated.firstBotCrea6ted;
    }
}
