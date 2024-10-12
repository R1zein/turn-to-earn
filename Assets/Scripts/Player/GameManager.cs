using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool firstBotCreated = false;
    public bool firstBuildCreated = false;
    public bool firstKillMaded = false;

    public static GameManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
}
