public class Enemy : NPCFacade
{
    private void Start()
    {
        navigation.FirstLook<BotMiner>();
        navigation.FirstLook<BotDefender>();
        navigation.ChaseTarget<Player>();
    }
    protected override void Navigation()
    {
        navigation.ChaseTarget<BotMiner>();
        navigation.ChaseTarget<BotDefender>();
        navigation.ChaseTarget<Player>();
    }
}
