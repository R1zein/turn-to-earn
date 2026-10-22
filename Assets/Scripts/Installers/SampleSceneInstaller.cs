using Zenject;

// The drone scene: the drone, its bombs, its HUD and a shooting range of standing dummies.
public class SampleSceneInstaller : GameplayInstaller
{
    protected override InputMode BaseInputMode => InputMode.Drone;

    protected override void InstallSceneBindings()
    {
        Container.Bind<DroneControl>().FromComponentInHierarchy().AsSingle();
        Container.Bind<DroneBombBay>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ShootingRange>().FromComponentInHierarchy().AsSingle();

        Container.Bind<EnemySpawner>().AsSingle();
        Container.Bind<BombSpawner>().AsSingle();
        Container.Bind<Explosions>().AsSingle();
        Container.BindInterfacesTo<ShootingRangeService>().AsSingle();

        Container.BindInterfacesTo<DroneHudController>().AsSingle();
    }
}
