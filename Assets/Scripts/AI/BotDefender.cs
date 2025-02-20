using UnityEngine;

public class BotDefender : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<Enemy>(10);
        navigation.SetAndRefresh();

    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<Enemy>(50);
        navigation.SetAndRefresh();

    }
}
