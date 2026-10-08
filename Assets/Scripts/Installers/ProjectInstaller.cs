using Zenject;

// Bindings that outlive a scene: configs, Input System controls, settings, audio.
// Empty until the first of them appears (GameConfig, Controls).
public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
    }
}
