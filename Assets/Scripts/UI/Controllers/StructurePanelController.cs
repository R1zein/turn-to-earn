using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Bottom right: icon, name and health of the structure the player is looking at,
// and, within reach of E, what E opens - the tabs of its window (MODIFY on a wall).
// Hidden when nothing is aimed at or a panel holds the mouse.
public class StructurePanelController : IInitializable, ITickable
{
    private const float Reach = 12f;

    [Inject] private UIDocument document;
    [Inject] private Camera camera;
    [Inject] private InputService input;
    [Inject] private StructureLookup structures;
    [Inject] private StructureWindowController window;

    private VisualElement panel;
    private VisualElement icon;
    private Label title;
    private Label health;
    private VisualElement fill;
    private VisualElement hint;
    private Label hintText;
    private Structure captioned;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        panel = root.Q("StructurePanel");
        icon = root.Q("StructureIcon");
        title = root.Q<Label>("StructureTitle");
        health = root.Q<Label>("StructureHealth");
        fill = root.Q("StructureHealthFill");
        hint = root.Q("StructureHint");
        hintText = root.Q<Label>("StructureHintText");
    }

    public void Tick()
    {
        Structure structure = Aimed(out float distance);
        panel.EnableInClassList("hidden", structure == null);
        if (structure == null)
            return;

        StatsHandler stats = structure.Stats;
        float hp = Mathf.Clamp(stats.currHP, 0f, stats.maxHP);
        title.text = structure.Title;
        icon.style.backgroundImage = new StyleBackground(structure.Icon);
        health.text = Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(stats.maxHP);
        fill.style.width = Length.Percent(hp / stats.maxHP * 100f);

        // Captions are worked out once per structure, not every frame.
        if (structure != captioned)
        {
            captioned = structure;
            hintText.text = window.Captions(structure);
        }
        hint.EnableInClassList("hidden", distance > PlayerInteraction.Reach || hintText.text.Length == 0);
    }

    private Structure Aimed(out float distance)
    {
        distance = 0f;
        // The camera lives on the player and is destroyed with it on death.
        if (!camera || input.Mode != InputMode.Gameplay)
            return null;

        Ray ray = camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (!Physics.Raycast(ray, out RaycastHit hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return null;
        distance = hit.distance;
        return structures.At(hit);
    }
}
