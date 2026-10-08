using Zenject;

// Everything a gameplay scene needs regardless of which map it is.
// Each scene has its own subclass that adds the map-specific bindings.
//
// Order of bindings is initialization order. No order dependencies yet.
public abstract class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ResourceWallet>().AsSingle();

        InstallSceneBindings();
    }

    protected abstract void InstallSceneBindings();
}
