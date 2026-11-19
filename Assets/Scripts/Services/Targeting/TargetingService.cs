using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Assigns one target per seeker. Only this service changes marks.
public class TargetingService : ITickable
{
    private const float RescanInterval = 0.75f;

    private readonly HashSet<Targetable> targets = new HashSet<Targetable>();
    private readonly List<TargetSeeker> seekers = new List<TargetSeeker>();
    private readonly HashSet<TargetSeeker> registered = new HashSet<TargetSeeker>();
    private readonly Dictionary<TargetSeeker, Targetable> assignments = new Dictionary<TargetSeeker, Targetable>();
    private readonly Dictionary<Targetable, int> marks = new Dictionary<Targetable, int>();
    private readonly Queue<TargetSeeker> urgent = new Queue<TargetSeeker>();
    private readonly HashSet<TargetSeeker> queued = new HashSet<TargetSeeker>();
    private readonly HashSet<TargetSeeker> updated = new HashSet<TargetSeeker>();
    private readonly List<Targetable> deadTargets = new List<Targetable>();
    private readonly List<(Targetable body, float score)> ranked = new List<(Targetable, float)>();
    private readonly TargetReachability reachability = new TargetReachability();
    private int cursor;

    public void Register(Targetable target) => targets.Add(target);

    public void Register(TargetSeeker seeker)
    {
        if (!registered.Add(seeker))
            return;
        seekers.Add(seeker);
        RequestRescan(seeker);
    }

    public void Unregister(Targetable target)
    {
        if (!targets.Remove(target))
            return;
        reachability.Forget(target);

        var affected = new List<TargetSeeker>();
        foreach (var assignment in assignments)
            if (ReferenceEquals(assignment.Value, target))
                affected.Add(assignment.Key);

        foreach (TargetSeeker seeker in affected)
        {
            Assign(seeker, null);
            RequestRescan(seeker);
        }
        marks.Remove(target);
    }

    public void Unregister(TargetSeeker seeker)
    {
        if (!registered.Remove(seeker))
            return;

        int index = seekers.IndexOf(seeker);
        seekers.RemoveAt(index);
        if (index < cursor)
            cursor--;
        queued.Remove(seeker);
        reachability.Forget(seeker);
        Assign(seeker, null);
    }

    public void RequestRescan(TargetSeeker seeker)
    {
        if (seeker != null && registered.Contains(seeker) && queued.Add(seeker))
            urgent.Enqueue(seeker);
    }

    public void ReportInterruptedPath(TargetSeeker seeker)
    {
        if (seeker.Current != null)
            reachability.Invalidate(seeker, seeker.Current);
        RequestRescan(seeker);
    }

    public void Tick()
    {
        RemoveDestroyedBodies();
        updated.Clear();

        // Leave requests raised by assignment callbacks for the next tick.
        int urgentCount = urgent.Count;
        for (int i = 0; i < urgentCount; i++)
        {
            TargetSeeker seeker = urgent.Dequeue();
            if (queued.Remove(seeker) && registered.Contains(seeker) && updated.Add(seeker))
                Choose(seeker);
        }

        int length = seekers.Count;
        int budget = Mathf.CeilToInt(length * Time.deltaTime / RescanInterval);
        for (int visited = 0; visited < length && budget > 0 && seekers.Count > 0; visited++)
        {
            if (cursor >= seekers.Count)
                cursor = 0;
            TargetSeeker seeker = seekers[cursor++];
            if (updated.Add(seeker))
            {
                Choose(seeker);
                budget--;
            }
        }
    }

    private void Choose(TargetSeeker seeker)
    {
        if (seeker == null || !seeker.isActiveAndEnabled)
            return;

        assignments.TryGetValue(seeker, out Targetable current);
        TargetProfile profile = seeker.Profile;
        NPCMotor motor = seeker.Motor;
        if (motor != null && (motor.HasPortalOrder || motor.IsTraversingLink || motor.IsAirborne))
            return;
        if (profile.NeedsPath && motor == null)
        {
            Assign(seeker, null);
            return;
        }
        if (profile.NeedsPath && !motor.CanPlanRoute)
            return;
        Vector3 origin = seeker.Origin;
        ranked.Clear();
        foreach (Targetable target in targets)
        {
            if (target == null || !target.IsAvailable || target.transform == seeker.transform || !profile.Targets(target.Faction)
                || !profile.TryGetWeight(target.Type, out float weight))
                continue;

            float distance = Vector3.Distance(origin, target.ClosestPoint(origin));
            if (profile.MaxDistance > 0f && distance > profile.MaxDistance)
                continue;

            marks.TryGetValue(target, out int load);
            bool isCurrent = target == current;
            float crowd = Mathf.Max(0, load - (isCurrent ? 1 : 0)) / (float)target.Capacity;
            float score = (weight + target.WeightOffset) * profile.DistanceMultiplier(distance)
                * profile.CrowdMultiplier(crowd) * (isCurrent ? profile.HoldBonus : 1f);
            if (score > 0f && float.IsFinite(score))
                ranked.Add((target, score));
        }

        ranked.Sort((left, right) => right.score.CompareTo(left.score));
        Targetable next = null;
        Vector3? navigationPoint = null;
        foreach (var candidate in ranked)
        {
            if (!seeker.CanSelect(candidate.body))
                continue;
            if (profile.NeedsPath)
            {
                if (!reachability.TryGetDestination(seeker, candidate.body, out Vector3 reachablePoint))
                    continue;
                navigationPoint = reachablePoint;
            }
            next = candidate.body;
            break;
        }
        seeker.NavigationPoint = navigationPoint;
        Assign(seeker, next);
    }

    private void Assign(TargetSeeker seeker, Targetable next)
    {
        assignments.TryGetValue(seeker, out Targetable previous);
        if (ReferenceEquals(previous, next))
            return;

        if (!ReferenceEquals(previous, null) && marks.TryGetValue(previous, out int oldCount))
        {
            if (oldCount == 1)
                marks.Remove(previous);
            else
                marks[previous] = oldCount - 1;
        }

        if (next == null)
            assignments.Remove(seeker);
        else
        {
            assignments[seeker] = next;
            marks.TryGetValue(next, out int count);
            marks[next] = count + 1;
        }

        if (seeker != null)
        {
            if (next == null)
                seeker.NavigationPoint = null;
            seeker.Assign(next);
        }
    }

    private void RemoveDestroyedBodies()
    {
        foreach (Targetable target in targets)
            if (target == null)
                deadTargets.Add(target);
        foreach (Targetable target in deadTargets)
            Unregister(target);
        deadTargets.Clear();

        for (int i = seekers.Count - 1; i >= 0; i--)
            if (seekers[i] == null)
                Unregister(seekers[i]);
    }
}
