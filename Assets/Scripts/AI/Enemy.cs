public class Enemy : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<BotMiner>();
        navigation.FirstLook<BotDefender>();
        navigation.FirstLook<Player>();
        navigation.FirstLook<GatlingGun>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotMiner>();
        navigation.ChaseTarget<BotDefender>();
        navigation.ChaseTarget<Player>();
        navigation.ChaseTarget<GatlingGun>();
    }
}
