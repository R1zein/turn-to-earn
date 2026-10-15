using System;
using UnityEngine.UIElements;
using Zenject;

public class ClockHudController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private TimeManager timeManager;

    private Label clock;

    public void Initialize()
    {
        clock = document.rootVisualElement.Q<Label>("ClockValue");
        timeManager.OnClockChanged += Redraw;
    }

    public void Dispose() => timeManager.OnClockChanged -= Redraw;

    // Minutes are not zero-padded, as before: 7:05 reads "7:5".
    private void Redraw() => clock.text = $"{timeManager.Hours}:{timeManager.Minutes}";
}
