using UnityEngine;
using Zenject;

// The main FPS map: player, resource nodes, shop and building, day and night.
//
// Order of bindings is initialization order:
//  1. DayCycle and TimeManager before everyone subscribed to time
//     (DayLighting, WaveService).
//  2. GameStarter last: it starts the clock after all Initialize calls.
public class Scene5Installer : GameplayInstaller
{
    protected override InputMode BaseInputMode => InputMode.Gameplay;

    protected override void InstallSceneBindings()
    {
        // Single instances already placed in the scene. The only camera is the
        // player's; ShopController lives on Canvas.
        Container.Bind<LevelAnchors>().FromComponentInHierarchy().AsSingle();
        Container.Bind<Camera>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ShopController>().FromComponentInHierarchy().AsSingle();

        // Everything born at runtime is born by one of these.
        Container.Bind<EnemySpawner>().AsSingle();
        Container.Bind<BotSpawner>().AsSingle();
        Container.Bind<BuildingPlacer>().AsSingle();
        Container.BindInterfacesAndSelfTo<ResourceNodeSpawner>().AsSingle();

        Container.Bind<ShopService>().AsSingle();
        Container.Bind<BuildService>().AsSingle();
        Container.BindInterfacesTo<PlayerInteraction>().AsSingle();

        Container.Bind<DayCycle>().AsSingle();
        Container.BindInterfacesAndSelfTo<TimeManager>().AsSingle();
        Container.BindInterfacesTo<DayLighting>().AsSingle();
        Container.BindInterfacesTo<WaveService>().AsSingle();

        Container.BindInterfacesTo<GameStarter>().AsSingle();
    }
}
