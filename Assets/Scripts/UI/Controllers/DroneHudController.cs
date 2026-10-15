using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Drone HUD: the aim reticle follows the cursor, the telemetry panel shows speed,
// altitude above world zero and total distance flown - numbers the drone measures itself.
public class DroneHudController : IInitializable, ITickable
{
    [Inject] private UIDocument document;
    [Inject] private DroneControl drone;
    [Inject] private InputService input;

    private VisualElement scope;
    private Label speed, altitude, distance;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        scope = root.Q("Scope");
        speed = root.Q<Label>("SpeedValue");
        altitude = root.Q<Label>("AltitudeValue");
        distance = root.Q<Label>("DistanceValue");
    }

    public void Tick()
    {
        PlaceAtScreenPoint(scope, input.DroneAim);

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
