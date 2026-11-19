using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Turn To Earn/Target Profile")]
public class TargetProfile : ScriptableObject
{
    [Serializable]
    private struct Weight
    {
        [SerializeField] private TargetType type;
        [SerializeField] private float weight;

        public TargetType Type => type;
        public float Value => weight;
    }

    [SerializeField] private string displayName;
    [SerializeField] private Fraction[] targetFractions = Array.Empty<Fraction>();
    [SerializeField] private List<Weight> typeWeights = new List<Weight>();
    [SerializeField] private AnimationCurve distanceCurve = AnimationCurve.Linear(0f, 1f, 100f, 0.02f);
    [SerializeField] private AnimationCurve crowdCurve = AnimationCurve.Linear(0f, 1f, 2f, 0.15f);
    [SerializeField, Min(1f)] private float holdBonus = 1.3f;
    [SerializeField, Min(0f), Tooltip("Metres. Zero means no limit.")] private float maxDistance;
    [SerializeField] private bool needsPath = true;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float HoldBonus => holdBonus;
    public float MaxDistance => maxDistance;
    public bool NeedsPath => needsPath;

    public bool Targets(Fraction fraction) => Array.IndexOf(targetFractions, fraction) >= 0;

    public bool TryGetWeight(TargetType type, out float value)
    {
        foreach (Weight entry in typeWeights)
        {
            if (entry.Type == type)
            {
                value = entry.Value;
                return true;
            }
        }

        value = 0f;
        return false;
    }

    public float DistanceMultiplier(float distance) => distanceCurve.Evaluate(distance);
    public float CrowdMultiplier(float load) => crowdCurve.Evaluate(load);
}
