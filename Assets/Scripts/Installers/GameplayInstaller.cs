using UnityEngine;
using UnityEngine.UIElements;
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

        // The scene's only UI document and only camera.
        Container.Bind<UIDocument>().FromComponentInHierarchy().AsSingle();
        Container.Bind<Camera>().FromComponentInHierarchy().AsSingle();

        Container.Bind<ResourceWallet>().AsSingle();

        // Bodies add themselves to these in OnEnable, before any Initialize.
        Container.Bind<TargetRegistry>().AsSingle();
        Container.Bind<TargetingService>().AsSingle();
        Container.Bind<HealthBars>().AsSingle();

        InstallSceneBindings();

        Container.BindInterfacesTo<WorldMarkersController>().AsSingle();
    }

    protected abstract void InstallSceneBindings();
}
