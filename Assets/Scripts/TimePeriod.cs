using System;
using UnityEngine;

[CreateAssetMenu(fileName = "My Data", menuName = "Scriptable Objects/TimePeriod")]
public class TimePeriod : ScriptableObject
{
    [Range(0, 24)] public int periodStart;
    [Range(0, 24)] public int periodEnd;
    public float currentProgress;
    public Material skyboxMaterial;
    public AudioClip soundEffect;
    public AnimationCurve curve;
    public int dayNumber = 0;

    private ReflectionProbe reflectionProbe;
    private Light directionalLight;
    private AudioSource source;
    private float duration;
    private float secondsPerHour;
    private bool wasInPeriod;

    public event Action OnPeriodEnter;

    public void InitSettings(float secondsPerHour, AudioSource source, Light directionalLight, ReflectionProbe reflectionProbe)
    {
        this.secondsPerHour = secondsPerHour;
        this.source = source;
        this.directionalLight = directionalLight;
        this.reflectionProbe = reflectionProbe;
        currentProgress = 0;
        wasInPeriod = false;
        dayNumber = 0;
    }
    public void ProgressTime(float time)
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

        if (!currentlyInPeriod)
        {
            wasInPeriod = false;
            currentProgress = 0;
            return;
        }

        if (!wasInPeriod)
        {
            PeriodEnter();
            wasInPeriod = true;
        }

        float t = time < startTime && startTime > endTime
            ? time + 24 * secondsPerHour
            : time;

        float end = startTime < endTime ? endTime : endTime + 24 * secondsPerHour;
        currentProgress = Mathf.InverseLerp(startTime, end, t);

        directionalLight.intensity = curve.Evaluate(currentProgress);
        reflectionProbe.intensity = curve.Evaluate(currentProgress);
    }
    private void PeriodEnter()
    {
        dayNumber++;
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
        OnPeriodEnter?.Invoke();
    }
}
