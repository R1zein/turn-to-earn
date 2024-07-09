using UnityEngine;

public class BotDefender : NPCFacade
{
    private NPCNavigation navigation;
    private void Start()
    {
        navigation = GetComponent<NPCNavigation>();
        navigation.FirstLook<Enemy>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<Enemy>();
    }
}
