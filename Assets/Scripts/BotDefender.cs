using UnityEngine;

public class BotDefender : NPSFasade
{
    private NPSNavigation navigation;
    private void Start()
    {
        navigation = GetComponent<NPSNavigation>();
        navigation.FirstLook<Enemy>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<Enemy>();
    }
}
