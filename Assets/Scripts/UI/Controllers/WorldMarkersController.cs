using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// A screen-space health bar over every HealthBarAnchor: each frame it moves to the
// projected point, so it reads the same at any distance. Hidden behind the camera
// and further than GameConfig.HealthBarDistance.
public class WorldMarkersController : IInitializable, ILateTickable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private UITemplate templates;
    [Inject] private HealthBars bars;
    [Inject] private Camera camera;
    [Inject] private GameConfig config;

    private VisualElement layer;
    private readonly Dictionary<HealthBarAnchor, (VisualElement marker, VisualElement fill)> markers =
        new Dictionary<HealthBarAnchor, (VisualElement, VisualElement)>();

    public void Initialize()
    {
        layer = document.rootVisualElement.Q("WorldMarkers");
        bars.OnAdded += Add;
        bars.OnRemoved += Remove;
        foreach (HealthBarAnchor anchor in bars.All)
            Add(anchor);
    }

    public void Dispose()
    {
        bars.OnAdded -= Add;
        bars.OnRemoved -= Remove;
    }

    public void LateTick()
    {
        // The player's camera is destroyed with the player on death.
        bool hasCamera = camera;
        foreach (var pair in markers)
        {
            HealthBarAnchor anchor = pair.Key;
            VisualElement marker = pair.Value.marker;

            Vector3 point = anchor.transform.position;
            bool isVisible = hasCamera
                && camera.WorldToViewportPoint(point).z > 0f   // behind the camera the projection mirrors
                && Vector3.Distance(camera.transform.position, point) <= config.HealthBarDistance;

            marker.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isVisible)
                continue;

            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, point, camera);
            marker.style.left = position.x;
            marker.style.top = position.y;

            StatsHandler stats = anchor.Stats;
            pair.Value.fill.style.width = Length.Percent(Mathf.Clamp01(stats.currHP / stats.maxHP) * 100f);
        }
    }

    private void Add(HealthBarAnchor anchor)
    {
        var marker = templates.HealthMarker.Instantiate().Q("Marker");
        layer.Add(marker);
        markers.Add(anchor, (marker, marker.Q("Fill")));
    }

    private void Remove(HealthBarAnchor anchor)
    {
        markers[anchor].marker.RemoveFromHierarchy();
        markers.Remove(anchor);
    }
}
