using System;
using UnityEngine;
using Zenject;

// How the world looks at every moment of the day, from one continuous clock
// rather than per period, so nothing jumps at a phase boundary:
//  - the sun rises at the start of dawn, peaks at noon and sets at the end of
//    dusk; at night the same light is a dim moon at a fixed angle. It swaps
//    direction at the horizon, where its intensity curve is ~0;
//  - sun colour and intensity, ambient and reflection intensity, fog colour and
//    sky exposure come from the Sky curves of GameConfig (whole day, 0..24 h);
//  - the day and night skyboxes swap at sunrise and sunset, where the exposure
//    curve is low. Runtime copies are used, so the assets are never written.
public class DayLighting : IInitializable, ITickable, IDisposable
{
    [Inject] private TimeManager timeManager;
    [Inject] private GameConfig config;
    [Inject] private LevelAnchors anchors;

    private Material daySky;
    private Material nightSky;
    private bool? isDaySkyUp;
    // The scene's own reflection settings; the ambient curve scales them.
    private float baseReflection;
    private float baseProbe;

    public void Initialize()
    {
        daySky = new Material(config.DaySkybox);
        nightSky = new Material(config.NightSkybox);
        baseReflection = RenderSettings.reflectionIntensity;
        baseProbe = anchors.ReflectionProbe.intensity;
        Apply(timeManager.Hour);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(daySky);
        UnityEngine.Object.Destroy(nightSky);
    }

    public void Tick() => Apply(timeManager.Hour);

    private void Apply(float hour)
    {
        float sunrise = config.SunriseHour;
        float sunset = config.SunsetHour;
        bool isSunUp = hour >= sunrise && hour < sunset;

        Light sun = anchors.Sun;
        if (isSunUp)
        {
            float t = Mathf.InverseLerp(sunrise, sunset, hour);
            float elevation = config.NoonSunElevation * Mathf.Sin(Mathf.PI * t);
            float yaw = config.NoonSunYaw - 90f + 180f * t;
            sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0f);
        }
        else
        {
            sun.transform.rotation = Quaternion.Euler(config.MoonElevation, config.NoonSunYaw + 180f, 0f);
        }

        float dayFraction = hour / 24f;
        sun.color = config.SunColor.Evaluate(dayFraction);
        sun.intensity = config.SunIntensity.Evaluate(hour);

        float ambient = config.AmbientIntensity.Evaluate(hour);
        RenderSettings.ambientIntensity = ambient;
        RenderSettings.reflectionIntensity = baseReflection * ambient;
        anchors.ReflectionProbe.intensity = baseProbe * ambient;
        RenderSettings.fogColor = config.FogColor.Evaluate(dayFraction);

        if (isDaySkyUp != isSunUp)
        {
            isDaySkyUp = isSunUp;
            RenderSettings.skybox = isSunUp ? daySky : nightSky;
            DynamicGI.UpdateEnvironment();
        }
        RenderSettings.skybox.SetFloat("_Exposure", config.SkyExposure.Evaluate(hour));
    }
}
