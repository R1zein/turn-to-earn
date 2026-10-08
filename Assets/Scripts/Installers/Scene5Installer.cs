using UnityEngine;
using Zenject;

// The main FPS map: player, resource nodes, shop and building, day and night.
//
// Order of bindings is initialization order:
//  1. DayCycle and TimeManager before everyone subscribed to time (DayLighting).
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
        Container.Bind<ResourceSpawner>().FromComponentInHierarchy().AsSingle();

        Container.BindInterfacesTo<PlayerInteraction>().AsSingle();

        Container.Bind<DayCycle>().AsSingle();
        Container.BindInterfacesAndSelfTo<TimeManager>().AsSingle();
        Container.BindInterfacesTo<DayLighting>().AsSingle();

        Container.BindInterfacesTo<GameStarter>().AsSingle();
    }
}
