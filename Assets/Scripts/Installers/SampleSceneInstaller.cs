using Zenject;

// The drone scene.
public class SampleSceneInstaller : GameplayInstaller
{
    protected override InputMode BaseInputMode => InputMode.Drone;

    protected override void InstallSceneBindings()
    {
        Container.Bind<DroneControl>().FromComponentInHierarchy().AsSingle();
        Container.BindInterfacesTo<DroneHudController>().AsSingle();
    }
}
