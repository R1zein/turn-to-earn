using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Drone HUD: the reticle marks where the nose (and the lasers) points, the
// telemetry panel shows the flight mode, speed, altitude above world zero and
// total distance flown - numbers the drone measures itself.
public class DroneHudController : IInitializable, ITickable
{
    // How far ahead of the drone the nose marker is projected, metres.
    private const float NoseMarkerDistance = 100f;

    [Inject] private UIDocument document;
    [Inject] private DroneControl drone;
    [Inject] private Camera camera;

    private VisualElement scope;
    private Label mode, speed, altitude, distance;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        scope = root.Q("Scope");
        mode = root.Q<Label>("ModeValue");
        speed = root.Q<Label>("SpeedValue");
        altitude = root.Q<Label>("AltitudeValue");
        distance = root.Q<Label>("DistanceValue");
    }

    public void Tick()
    {
        Transform body = drone.transform;
        Vector3 screen = camera.WorldToScreenPoint(body.position + body.forward * NoseMarkerDistance);
        // Behind the camera (mid-flip) there is no sensible place for it.
        scope.style.visibility = screen.z > 0f ? Visibility.Visible : Visibility.Hidden;
        if (screen.z > 0f)
            PlaceAtScreenPoint(scope, screen);

        mode.text = drone.IsAcro ? "ACRO" : "STAB";
        // Invariant culture keeps a dot separator regardless of regional settings.
        speed.text = drone.GetSpeed().ToString("F1", CultureInfo.InvariantCulture) + " M/S";
        altitude.text = drone.GetAltitude().ToString("F1", CultureInfo.InvariantCulture) + " M";
        distance.text = drone.GetDistanceTravelled().ToString("F0", CultureInfo.InvariantCulture) + " M";
    }

    // Screen space has Y running up from the bottom, the panel has it running down from
    // the top, hence the flip. ScreenToPanel then undoes the panel's own scaling.
    // The element carries translate: -50% -50%, so left/top place its centre on the point.
    private static void PlaceAtScreenPoint(VisualElement element, Vector2 screenPoint)
    {
        Vector2 local = RuntimePanelUtils.ScreenToPanel(
            element.panel, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
        element.style.left = local.x;
        element.style.top = local.y;
    }
}
