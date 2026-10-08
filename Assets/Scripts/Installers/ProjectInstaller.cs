using System;
using UnityEngine;
using Zenject;

// Bindings that outlive a scene: configs, Input System controls, settings, audio.
public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private GameConfig gameConfig;

    public override void InstallBindings()
    {
        Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();
        // Generated from Assets/Input/Controls.inputactions; only InputService reads it.
        Container.Bind(typeof(Controls), typeof(IDisposable)).To<Controls>().AsSingle();
    }
}
