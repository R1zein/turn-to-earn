using UnityEngine;
using Zenject;

// Bindings that outlive a scene: configs, Input System controls, settings, audio.
public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private UITemplate uiTemplate;

    public override void InstallBindings()
    {
        Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();
        Container.Bind<UITemplate>().FromInstance(uiTemplate).AsSingle();
        // Generated from Assets/Input/Controls.inputactions; only InputService reads it.
        Container.BindInterfacesAndSelfTo<Controls>().AsSingle();
    }
}
