using UnityEngine;

public abstract class Quest : ScriptableObject
{
    public DialogData dialogData;
    public bool IsTaken = false;
    public int currentIndex = 0;
    public abstract bool IsQuestComplited();
}
