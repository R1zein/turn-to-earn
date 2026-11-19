using System;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [SerializeField] private TargetType type;
    [SerializeField] private float weightOffset;
    [SerializeField, Min(1)] private int capacity = 4;
    [Inject] private TargetingService targeting;

    private TargetGeometry geometry;
    private bool isUnavailable;

    public TargetType Type => type;
    public float WeightOffset => weightOffset;
    public int Capacity => Mathf.Max(1, capacity);
    public StatsHandler Health { get; private set; }
    public Fraction Faction => Health != null ? Health.fraction : Fraction.Neutral;
    public Vector3 AimPoint => geometry.AimPoint;
    public bool IsAvailable => isActiveAndEnabled && !isUnavailable && (Health == null || !Health.IsDead);

    private void Awake()
    {
        Health = GetComponent<StatsHandler>();
        geometry = new TargetGeometry(transform);
    }

    public Vector3 ClosestPoint(Vector3 from) => geometry.ClosestPoint(from);

    private void OnEnable()
    {
        if (!IsAvailable || GetComponentInParent<Ghost>() != null)
            return;
        if (type == null)
            throw new InvalidOperationException($"Missing target type on {name}");

        if (Health != null)
            Health.OnDeath += MakeUnavailable;
        targeting.Register(this);
    }

    private void OnDisable()
    {
        if (Health != null)
            Health.OnDeath -= MakeUnavailable;
        targeting.Unregister(this);
    }

    public void MakeUnavailable()
    {
        isUnavailable = true;
        targeting.Unregister(this);
    }
}
