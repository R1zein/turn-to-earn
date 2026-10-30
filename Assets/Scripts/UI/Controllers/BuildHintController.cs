using UnityEngine.UIElements;
using Zenject;

// One line at the bottom of the screen with the keys of what is going on: placing a
// ghost or dismantling. Hidden the rest of the time.
public class BuildHintController : IInitializable, ITickable
{
    [Inject] private UIDocument document;
    [Inject] private BuildService buildService;
    [Inject] private Dismantler dismantler;

    private Label hint;

    public void Initialize()
    {
        hint = document.rootVisualElement.Q<Label>("BuildHint");
    }

    public void Tick()
    {
        string text = Text();
        hint.EnableInClassList("hidden", text == null);
        if (text != null)
            hint.text = text;
    }

    private string Text()
    {
        if (dismantler.IsActive)
            return "F - DISMANTLE     TAB - STOP";
        if (buildService.Current == null)
            return null;
        if (!buildService.IsOnGrid)
            return "WHEEL - TURN     F - PLACE";

        string height = buildService.HeightSteps > 0 ? "UP" : buildService.HeightSteps < 0 ? "DOWN" : "LEVEL";
        return "WHEEL - TURN     Z / X - HEIGHT: " + height + "     F - PLACE";
    }
}
