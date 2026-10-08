using System;
using UnityEngine;
using Zenject;

// Applies the active period's look to the level: skybox on entry, sun and
// reflection probe intensity along the period's curve.
public class DayLighting : IInitializable, IDisposable
{
    [Inject] private DayCycle dayCycle;
    [Inject] private LevelAnchors anchors;

    public void Initialize()
    {
        dayCycle.OnPeriodEnter += ApplySkybox;
        dayCycle.OnPeriodProgress += ApplyIntensity;
    }

    public void Dispose()
    {
        dayCycle.OnPeriodEnter -= ApplySkybox;
        dayCycle.OnPeriodProgress -= ApplyIntensity;
    }

    private void ApplySkybox(TimePeriod period)
    {
        RenderSettings.skybox = period.skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }

    private void ApplyIntensity(TimePeriod period, float progress)
    {
        float intensity = period.curve.Evaluate(progress);
        anchors.Sun.intensity = intensity;
        anchors.ReflectionProbe.intensity = intensity;
    }
}
