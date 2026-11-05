using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Bottom right: icon, name and health of the structure the player is looking at.
// Hidden when nothing is aimed at or a panel holds the mouse.
public class StructurePanelController : IInitializable, ITickable
{
    private const float Reach = 12f;

    [Inject] private UIDocument document;
    [Inject] private Camera camera;
    [Inject] private InputService input;
    [Inject] private StructureLookup structures;

    private VisualElement panel;
    private VisualElement icon;
    private Label title;
    private Label health;
    private VisualElement fill;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        panel = root.Q("StructurePanel");
        icon = root.Q("StructureIcon");
        title = root.Q<Label>("StructureTitle");
        health = root.Q<Label>("StructureHealth");
        fill = root.Q("StructureHealthFill");
    }

    public void Tick()
    {
        Structure structure = Aimed();
        panel.EnableInClassList("hidden", structure == null);
        if (structure == null)
            return;

        StatsHandler stats = structure.Stats;
        float hp = Mathf.Clamp(stats.currHP, 0f, stats.maxHP);
        title.text = structure.Title;
        icon.style.backgroundImage = new StyleBackground(structure.Icon);
        health.text = Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(stats.maxHP);
        fill.style.width = Length.Percent(hp / stats.maxHP * 100f);
    }

    private Structure Aimed()
    {
        // The camera lives on the player and is destroyed with it on death.
        if (!camera || input.Mode != InputMode.Gameplay)
            return null;

        Ray ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (!Physics.Raycast(ray, out RaycastHit hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return null;
        return structures.At(hit);
    }
}
