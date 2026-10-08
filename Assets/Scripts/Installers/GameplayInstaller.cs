using Zenject;

// Everything a gameplay scene needs regardless of which map it is.
// Each scene has its own subclass that adds the map-specific bindings
// and says which input mode the scene starts in.
//
// Order of bindings is initialization order:
//  1. InputService first: it sets the cursor and action maps of the base mode,
//     and panels opened in later Initialize calls push on top of it.
public abstract class GameplayInstaller : MonoInstaller
{
    protected abstract InputMode BaseInputMode { get; }

    public override void InstallBindings()
    {
        Container.BindInstance(BaseInputMode).WhenInjectedInto<InputService>();
        Container.BindInterfacesAndSelfTo<InputService>().AsSingle();

        Container.Bind<ResourceWallet>().AsSingle();

        InstallSceneBindings();
    }

    protected abstract void InstallSceneBindings();
}
