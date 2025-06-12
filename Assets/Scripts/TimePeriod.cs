using UnityEngine;

[CreateAssetMenu(fileName = "My Data", menuName = "Scriptable Objects/TimePeriod")]
public class TimePeriod : ScriptableObject
{
    [Range(0, 24)] public int periodStart;
    [Range(0, 24)] public int periodEnd;
    public float currentProgress;
    public Material skyboxMaterial;
    public AudioClip soundEffect;
    
    private AudioSource source;
    private float duration;
    private float secondsPerHour;
    private bool isInPeriod;

    public void InitSettings(float secondsPerHour, AudioSource source)
    {
        this.secondsPerHour = secondsPerHour;
        this.source = source;
        currentProgress = 0;
        isInPeriod = false;
    }

    public bool ProgressTime(float time)
    {
        float startTime = periodStart * secondsPerHour;
        float endTime = periodEnd * secondsPerHour;

        bool currentlyInPeriod;

        if (startTime < endTime)
        {
            currentlyInPeriod = time >= startTime && time < endTime;
        }
        else
        {
            float adjustedTime = time < startTime ? time + 24 * secondsPerHour : time;
            float adjustedEnd = endTime + 24 * secondsPerHour;
            currentlyInPeriod = adjustedTime >= startTime && adjustedTime < adjustedEnd;
        }

        if (currentlyInPeriod)
        {
            if (!isInPeriod)
            {
                isInPeriod = true;
                OnPeriodEnter();
            }

            float t = time < startTime && startTime > endTime
                ? time + 24 * secondsPerHour
                : time;

            float end = startTime < endTime ? endTime : endTime + 24 * secondsPerHour;
            currentProgress = Mathf.InverseLerp(startTime, end, t);
            return true;
        }
        else
        {
            isInPeriod = false;
            return false;
        }
    }

    protected virtual void OnPeriodEnter()
    {
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
        source.clip = soundEffect;
        source.Play();
    }
}
