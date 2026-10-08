using UnityEngine;
using Zenject;

// Bindings that outlive a scene: configs, Input System controls, settings, audio.
public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private GameConfig gameConfig;

    public override void InstallBindings()
    {
        Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();
    }
}
