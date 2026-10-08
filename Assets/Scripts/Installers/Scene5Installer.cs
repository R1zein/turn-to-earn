using Zenject;

// The main FPS map: player, resource nodes, shop and building.
//
// Order of bindings is initialization order. No order dependencies yet.
public class Scene5Installer : GameplayInstaller
{
    protected override void InstallSceneBindings()
    {
        // Single instances already placed in the scene (ShopController lives on Canvas).
        Container.Bind<ShopController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ResourceSpawner>().FromComponentInHierarchy().AsSingle();
    }
}
