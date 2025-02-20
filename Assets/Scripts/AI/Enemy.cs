public class Enemy : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<BotMiner>(10);
        navigation.FirstLook<BotDefender>(50);
        navigation.FirstLook<Player>(75);
        navigation.FirstLook<GatlingGun>(25);
        navigation.SetAndRefresh();

    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotMiner>(10);
        navigation.ChaseTarget<BotDefender>(50);
        navigation.ChaseTarget<Player>(75);
        navigation.ChaseTarget<GatlingGun>(25);
        navigation.SetAndRefresh();

    }
}
