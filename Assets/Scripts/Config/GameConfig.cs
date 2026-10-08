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

    public float SecondsPerHour => secondsPerHour;
    public int StartHour => startHour;
    public IReadOnlyList<TimePeriod> DayPeriods => dayPeriods;
}
