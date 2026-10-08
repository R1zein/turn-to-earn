using System;
using UnityEngine;
using Zenject;

// The world clock. Stands still until GameStarter calls Begin.
public class TimeManager : ITickable
{
    [Inject] private GameConfig config;
    [Inject] private DayCycle dayCycle;

    private float timer;   // seconds since midnight
    private bool isRunning;

    public int Hours { get; private set; }
    public int Minutes { get; private set; }
    public event Action OnClockChanged;

    public void Begin()
    {
        timer = config.StartHour * config.SecondsPerHour;
        isRunning = true;
    }

    public void Tick()
    {
        if (!isRunning)
            return;

        float secondsPerHour = config.SecondsPerHour;
        timer += Time.deltaTime;
        if (timer >= secondsPerHour * 24)
            timer = 0;

        int hours = (int)(timer / secondsPerHour);
        int minutes = (int)((timer % secondsPerHour) / (secondsPerHour / 60));
        if (hours != Hours || minutes != Minutes)
        {
            Hours = hours;
            Minutes = minutes;
            OnClockChanged?.Invoke();
        }

        dayCycle.Advance(timer / secondsPerHour);
    }
}
