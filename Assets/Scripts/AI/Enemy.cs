public class Enemy : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<BotMiner>(50);
        navigation.FirstLook<BotDefender>(70);
        navigation.FirstLook<Player>(75);
        navigation.FirstLook<GatlingGun>(60);
        navigation.FirstLook<Building>(30);
        navigation.SetAndRefresh();

    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotMiner>(50);
        navigation.ChaseTarget<BotDefender>(70);
        navigation.ChaseTarget<Player>(75);
        navigation.ChaseTarget<GatlingGun>(60);
        navigation.FirstLook<Building>(30);
        navigation.SetAndRefresh();

    }
}
