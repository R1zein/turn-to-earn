using System;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public class TargetSeeker : MonoBehaviour
{
    [SerializeField] private TargetProfile profile;
    [Inject] private TargetingService targeting;

    private StatsHandler health;
    private Transform origin;
    private Predicate<Targetable> isCandidate;

    public TargetProfile Profile => profile;
    public Targetable Current { get; private set; }
    public Vector3 Origin => origin != null ? origin.position : transform.position;
    public NPCMotor Motor { get; private set; }
    public Vector3? NavigationPoint { get; internal set; }
    public event Action<Targetable> OnTargetChanged;

    private void Awake()
    {
        health = GetComponent<StatsHandler>();
        Motor = GetComponent<NPCMotor>();
    }

    private void OnEnable()
    {
        if (GetComponentInParent<Ghost>() != null || (health != null && health.IsDead))
            return;
        if (profile == null)
            throw new InvalidOperationException($"Missing target profile on {name}");

        if (health != null)
            health.OnDeath += Leave;
        if (Motor != null)
            Motor.PathInterrupted += OnPathInterrupted;
        targeting.Register(this);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= Leave;
        if (Motor != null)
            Motor.PathInterrupted -= OnPathInterrupted;
        Leave();
    }

    private void Leave() => targeting.Unregister(this);

    private void OnPathInterrupted() => targeting.ReportInterruptedPath(this);

    public void RequestRescan() => targeting.RequestRescan(this);

    public void ConfigureSight(Transform muzzle, Predicate<Targetable> filter)
    {
        origin = muzzle;
        isCandidate = filter;
    }

    internal bool CanSelect(Targetable candidate) => isCandidate == null || isCandidate(candidate);

    public bool IsInRange(Targetable target)
    {
        if (target == null || !target.IsAvailable || !profile.Targets(target.Faction))
            return false;
        float max = profile.MaxDistance;
        return max == 0f || (target.ClosestPoint(Origin) - Origin).sqrMagnitude <= max * max;
    }

    internal void Assign(Targetable next)
    {
        Current = next;
        OnTargetChanged?.Invoke(next);
    }
}
