public class BotDefender : NPCFacade
{
    protected override TargetKind? Kind => TargetKind.BotDefender;

    private void Start()
    {
        navigation.LookEverywhere(config.DefenderTargets);
    }
    protected override void Navigation()
    {
        navigation.Rescan(config.DefenderTargets);
    }
}
