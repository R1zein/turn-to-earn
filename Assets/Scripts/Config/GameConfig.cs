using System.Collections.Generic;
using UnityEngine;

// Game-wide tuning. Read-only at runtime: nothing writes into this asset.
[CreateAssetMenu(menuName = "Data/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Time")]
    [SerializeField] private float secondsPerHour = 20;
    [SerializeField, Range(0, 24)] private int startHour = 7;
    [SerializeField] private TimePeriod[] dayPeriods;

    [Header("Sky (curves and gradients span the whole day: hour 0..24, gradient 0..1)")]
    [Tooltip("The sun rises when this period begins")]
    [SerializeField] private TimePeriod dawnPeriod;
    [Tooltip("The sun sets when this period ends")]
    [SerializeField] private TimePeriod duskPeriod;
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material nightSkybox;
    [SerializeField, Range(0, 90)] private float noonSunElevation = 55;
    [SerializeField, Range(0, 90)] private float moonElevation = 40;
    [Tooltip("Compass direction of the sun at noon; it rises 90 deg before and sets 90 deg after")]
    [SerializeField] private float noonSunYaw = 307;
    [SerializeField] private Gradient sunColor = new Gradient();
    [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.Constant(0, 24, 1);
    [SerializeField] private AnimationCurve ambientIntensity = AnimationCurve.Constant(0, 24, 1);
    [Tooltip("Exposure of whichever skybox is up; keep it low at sunrise and sunset, where they swap")]
    [SerializeField] private AnimationCurve skyExposure = AnimationCurve.Constant(0, 24, 1);
    [SerializeField] private Gradient fogColor = new Gradient();

    [Header("Waves")]
    [Tooltip("A wave starts at every portal each time this period begins")]
    [SerializeField] private TimePeriod wavePeriod;
    [SerializeField] private Enemy waveEnemy;
    [Tooltip("Enemies per portal on the first wave; each wave adds 2 per wave number")]
    [SerializeField] private int waveBaseCount = 5;
    [SerializeField] private float waveSpawnInterval = 1;
    [Tooltip("Heavy enemy that joins the waves from wave Spider First Wave on")]
    [SerializeField] private Enemy spiderEnemy;
    [Tooltip("Number of the first wave (1-based) with spiders")]
    [SerializeField] private int spiderFirstWave = 3;
    [Tooltip("Spiders per wave, across all portals, added for every wave from the first spider wave on")]
    [SerializeField] private int spidersPerWave = 1;

    [Header("Drone bomb")]
    [SerializeField] private DroneBomb bombPrefab;
    [Tooltip("Seconds between bombs")]
    [SerializeField] private float bombCooldown = 30f;
    [Tooltip("Blast radius, m. With the damages below a 100 HP zombie dies within ~7 m: a crowd of ten")]
    [SerializeField] private float bombRadius = 9f;
    [SerializeField] private float bombCenterDamage = 300f;
    [Tooltip("Damage right at the edge of the radius; it falls linearly from the centre")]
    [SerializeField] private float bombEdgeDamage = 50f;
    [Tooltip("Visual spawned at the blast")]
    [SerializeField] private GameObject bombEffect;
    [Tooltip("Scale of the visual: its flash is 4 m across at 1, so 2.5 roughly matches the radius")]
    [SerializeField] private float bombEffectScale = 2.5f;

    [Header("Shooting range (drone scene)")]
    [Tooltip("Dummy placed on every spot of the range; it stands still")]
    [SerializeField] private Enemy rangeTarget;
    [Tooltip("Seconds before a killed dummy is replaced")]
    [SerializeField] private float rangeRespawnDelay = 3f;

    [Header("Targeting (score = weight / distance)")]
    [SerializeField] private TargetWeight[] enemyTargets;
    [SerializeField] private TargetWeight[] bomberTargets;
    [SerializeField] private TargetWeight[] minerTargets;
    [SerializeField] private TargetWeight[] defenderTargets;

    [Header("Shop and building")]
    [SerializeField] private BotOffer[] botOffers;
    [SerializeField] private BuildOption[] buildOptions;
    [SerializeField] private FortificationOption[] fortificationOptions;
    [SerializeField] private FortificationPartOption[] fortificationParts;

    [Header("Water")]
    [Tooltip("Player speed while swimming (walk is 5, run 9)")]
    [SerializeField] private float swimSpeed = 2.5f;

    [Header("World markers")]
    [Tooltip("Health bars further than this from the camera are hidden")]
    [SerializeField] private float healthBarDistance = 30;

    [Header("Input")]
    // Measured 2026-10-08 on Windows with both input backends active: old
    // GetAxisRaw("Mouse X/Y") was exactly 0.05 of the Input System delta, every frame.
    [Tooltip("Input System mouse delta -> old Input Manager \"Mouse X/Y\" units, which the look sensitivity was tuned against")]
    [SerializeField] private float lookScale = 0.05f;
    // Input System normalizes scroll to +-1 per notch, same as old mouseScrollDelta on Windows.
    [Tooltip("Input System scroll -> old Input.mouseScrollDelta units")]
    [SerializeField] private float scrollScale = 1f;
    [Tooltip("Drone: on-screen cursor pixels per pixel of mouse movement")]
    [SerializeField] private float droneCursorSpeed = 1f;
    [Tooltip("Drone aiming (RMB): degrees the aim turns per pixel of mouse movement")]
    [SerializeField] private float droneAimSensitivity = 0.1f;

    public float SecondsPerHour => secondsPerHour;
    public int StartHour => startHour;
    public IReadOnlyList<TimePeriod> DayPeriods => dayPeriods;

    public float SunriseHour => dawnPeriod.periodStart;
    public float SunsetHour => duskPeriod.periodEnd;
    public Material DaySkybox => daySkybox;
    public Material NightSkybox => nightSkybox;
    public float NoonSunElevation => noonSunElevation;
    public float MoonElevation => moonElevation;
    public float NoonSunYaw => noonSunYaw;
    public Gradient SunColor => sunColor;
    public AnimationCurve SunIntensity => sunIntensity;
    public AnimationCurve AmbientIntensity => ambientIntensity;
    public AnimationCurve SkyExposure => skyExposure;
    public Gradient FogColor => fogColor;

    public TimePeriod WavePeriod => wavePeriod;
    public Enemy WaveEnemy => waveEnemy;
    public int WaveBaseCount => waveBaseCount;
    public float WaveSpawnInterval => waveSpawnInterval;
    public Enemy SpiderEnemy => spiderEnemy;
    public int SpiderFirstWave => spiderFirstWave;
    public int SpidersPerWave => spidersPerWave;
    public DroneBomb BombPrefab => bombPrefab;
    public float BombCooldown => bombCooldown;
    public float BombRadius => bombRadius;
    public float BombCenterDamage => bombCenterDamage;
    public float BombEdgeDamage => bombEdgeDamage;
    public GameObject BombEffect => bombEffect;
    public float BombEffectScale => bombEffectScale;
    public Enemy RangeTarget => rangeTarget;
    public float RangeRespawnDelay => rangeRespawnDelay;

    public IReadOnlyList<TargetWeight> EnemyTargets => enemyTargets;
    public IReadOnlyList<TargetWeight> BomberTargets => bomberTargets;
    public IReadOnlyList<TargetWeight> MinerTargets => minerTargets;
    public IReadOnlyList<TargetWeight> DefenderTargets => defenderTargets;

    public IReadOnlyList<BotOffer> BotOffers => botOffers;
    public IReadOnlyList<BuildOption> BuildOptions => buildOptions;
    public IReadOnlyList<FortificationOption> FortificationOptions => fortificationOptions;
    public IReadOnlyList<FortificationPartOption> FortificationParts => fortificationParts;

    public float SwimSpeed => swimSpeed;
    public float HealthBarDistance => healthBarDistance;

    public float LookScale => lookScale;
    public float DroneCursorSpeed => droneCursorSpeed;
    public float DroneAimSensitivity => droneAimSensitivity;
    public float ScrollScale => scrollScale;
}
