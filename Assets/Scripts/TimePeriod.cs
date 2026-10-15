using UnityEngine;

// Read-only description of a phase of the day (dawn, day, dusk, night) for game
// logic: waves start on a period, the HUD names it. Runtime state (whether it is
// active, how many times it began) lives in DayCycle; how the world looks across
// the day is DayLighting's, driven by the Sky section of GameConfig.
[CreateAssetMenu(fileName = "My Data", menuName = "Scriptable Objects/TimePeriod")]
public class TimePeriod : ScriptableObject
{
    [Range(0, 24)] public float periodStart;
    [Range(0, 24)] public float periodEnd;
    public AudioClip soundEffect;

    // hour is 0..24 since midnight. A period whose start is not before its end
    // (e.g. 21 -> 6) runs past midnight, so hours after midnight are shifted by a day.
    public bool TryGetProgress(float hour, out float progress)
    {
        float start = periodStart;
        float end = periodEnd;
        if (periodStart >= periodEnd)
        {
            end += 24;
            if (hour < start)
                hour += 24;
        }

        if (hour < start || hour >= end)
        {
            progress = 0;
            return false;
        }

        progress = Mathf.InverseLerp(start, end, hour);
        return true;
    }
}
