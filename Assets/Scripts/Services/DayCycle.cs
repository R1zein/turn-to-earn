using System;
using System.Collections.Generic;
using Zenject;

// Which parts of the day are active and how many times each has begun.
// Driven by TimeManager; lives in the scene container, so a reloaded scene
// starts counting nights from zero.
public class DayCycle
{
    [Inject] private GameConfig config;

    private readonly HashSet<TimePeriod> activePeriods = new HashSet<TimePeriod>();
    private readonly Dictionary<TimePeriod, int> enterCounts = new Dictionary<TimePeriod, int>();

    public event Action<TimePeriod> OnPeriodEnter;
    public event Action<TimePeriod> OnPeriodExit;
    // Progress 0..1 through an active period, raised every advance.
    public event Action<TimePeriod, float> OnPeriodProgress;

    // How many times the period has begun, counting the current one.
    public int EnterCount(TimePeriod period) =>
        enterCounts.TryGetValue(period, out int count) ? count : 0;

    public void Advance(float hour)
    {
        foreach (TimePeriod period in config.DayPeriods)
        {
            bool isInside = period.TryGetProgress(hour, out float progress);
            bool wasInside = activePeriods.Contains(period);

            if (isInside && !wasInside)
            {
                activePeriods.Add(period);
                enterCounts[period] = EnterCount(period) + 1;
                OnPeriodEnter?.Invoke(period);
            }
            else if (!isInside && wasInside)
            {
                activePeriods.Remove(period);
                OnPeriodExit?.Invoke(period);
            }

            if (isInside)
                OnPeriodProgress?.Invoke(period, progress);
        }
    }
}
