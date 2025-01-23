using UnityEngine;

public class BotDefender : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<Enemy>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<Enemy>();
    }
}
