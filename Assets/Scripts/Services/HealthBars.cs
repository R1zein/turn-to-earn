using System;
using System.Collections.Generic;

// Everything that shows a health bar over itself. Bodies add themselves while
// enabled; WorldMarkersController draws one marker per entry.
public class HealthBars
{
    private readonly HashSet<HealthBarAnchor> anchors = new HashSet<HealthBarAnchor>();

    public event Action<HealthBarAnchor> OnAdded;
    public event Action<HealthBarAnchor> OnRemoved;

    public IReadOnlyCollection<HealthBarAnchor> All => anchors;

    public void Add(HealthBarAnchor anchor)
    {
        if (anchors.Add(anchor))
            OnAdded?.Invoke(anchor);
    }

    public void Remove(HealthBarAnchor anchor)
    {
        if (anchors.Remove(anchor))
            OnRemoved?.Invoke(anchor);
    }
}
