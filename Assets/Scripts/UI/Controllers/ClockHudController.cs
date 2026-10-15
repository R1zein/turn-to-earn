using System;
using UnityEngine.UIElements;
using Zenject;

// Clock and the current phase of the day (DAWN, DAY, DUSK, NIGHT - the period asset's name).
public class ClockHudController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private TimeManager timeManager;
    [Inject] private DayCycle dayCycle;

    private Label clock;
    private Label phase;

    public void Initialize()
    {
        clock = document.rootVisualElement.Q<Label>("ClockValue");
        phase = document.rootVisualElement.Q<Label>("PhaseValue");
        timeManager.OnClockChanged += Redraw;
        dayCycle.OnPeriodEnter += ShowPhase;
    }

    public void Dispose()
    {
        timeManager.OnClockChanged -= Redraw;
        dayCycle.OnPeriodEnter -= ShowPhase;
    }

    // Minutes are zero-padded: 7:05, not 7:5.
    private void Redraw() => clock.text = $"{timeManager.Hours}:{timeManager.Minutes:00}";

    private void ShowPhase(TimePeriod period) => phase.text = period.name.ToUpperInvariant();
}
