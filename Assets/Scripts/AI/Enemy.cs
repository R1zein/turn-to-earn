public class Enemy : NPCFacade
{
    protected override TargetKind? Kind => TargetKind.Enemy;

    private void Start()
    {
        navigation.LookEverywhere(config.EnemyTargets);
    }
    protected override void Navigation()
    {
        navigation.Rescan(config.EnemyTargets);
    }
}
