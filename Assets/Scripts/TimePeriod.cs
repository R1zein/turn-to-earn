using UnityEngine;

// Read-only description of a part of the day. Runtime state (whether the period
// is active, how many times it began) lives in the DayCycle scene service.
[CreateAssetMenu(fileName = "My Data", menuName = "Scriptable Objects/TimePeriod")]
public class TimePeriod : ScriptableObject
{
    [Range(0, 24)] public int periodStart;
    [Range(0, 24)] public int periodEnd;
    public Material skyboxMaterial;
    public AudioClip soundEffect;
    public AnimationCurve curve;

    // hour is 0..24 since midnight. A period whose start is not before its end
    // (e.g. 23 -> 7) runs past midnight, so hours after midnight are shifted by a day.
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
