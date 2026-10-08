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

    [Header("Waves")]
    [Tooltip("A wave starts at every portal each time this period begins")]
    [SerializeField] private TimePeriod wavePeriod;
    [SerializeField] private Enemy waveEnemy;
    [Tooltip("Enemies per portal on the first wave; each wave adds 2 per wave number")]
    [SerializeField] private int waveBaseCount = 5;
    [SerializeField] private float waveSpawnInterval = 1;

    [Header("Input")]
    // Measured 2026-10-08 on Windows with both input backends active: old
    // GetAxisRaw("Mouse X/Y") was exactly 0.05 of the Input System delta, every frame.
    [Tooltip("Input System mouse delta -> old Input Manager \"Mouse X/Y\" units, which the look sensitivity was tuned against")]
    [SerializeField] private float lookScale = 0.05f;
    // Input System normalizes scroll to +-1 per notch, same as old mouseScrollDelta on Windows.
    [Tooltip("Input System scroll -> old Input.mouseScrollDelta units")]
    [SerializeField] private float scrollScale = 1f;

    public float SecondsPerHour => secondsPerHour;
    public int StartHour => startHour;
    public IReadOnlyList<TimePeriod> DayPeriods => dayPeriods;

    public TimePeriod WavePeriod => wavePeriod;
    public Enemy WaveEnemy => waveEnemy;
    public int WaveBaseCount => waveBaseCount;
    public float WaveSpawnInterval => waveSpawnInterval;

    public float LookScale => lookScale;
    public float ScrollScale => scrollScale;
}
