using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Drone HUD: the cyan ring sits exactly on the invisible steering cursor; the pink
// diamond stays static in the screen centre (it lives in the UXML, code never
// moves it), circled by the dead zone - sized from the drone's own radius every
// frame, so what you see is exactly where turning starts. While aiming there is
// no joystick: the ring marks where the mouse has pointed the aim (the nose
// snaps there) and the dead zone is hidden. The telemetry panel
// shows throttle, speed, altitude above world zero and total distance flown -
// numbers the drone measures itself; the key hint shows whether the cursor
// auto-centres.
public class DroneHudController : IInitializable, ITickable
{
    [Inject] private UIDocument document;
    [Inject] private DroneControl drone;
    [Inject] private Camera camera;

    // How far ahead the aim direction is projected while aiming, metres.
    private const float AimMarkerDistance = 100f;

    private VisualElement scope, deadZone;
    private Label throttle, speed, altitude, distance, autoCentre;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        scope = root.Q("Scope");
        deadZone = root.Q("DeadZone");
        autoCentre = root.Q<Label>("AutoCentreValue");
        throttle = root.Q<Label>("ThrottleValue");
        speed = root.Q<Label>("SpeedValue");
        altitude = root.Q<Label>("AltitudeValue");
        distance = root.Q<Label>("DistanceValue");
    }

    public void Tick()
    {
        if (drone.IsAiming)
        {
            Vector3 aim = camera.WorldToScreenPoint(drone.transform.position + drone.AimDirection * AimMarkerDistance);
            scope.style.visibility = aim.z > 0f ? Visibility.Visible : Visibility.Hidden;
            if (aim.z > 0f)
                PlaceAtScreenPoint(scope, aim);
        }
        else
        {
            scope.style.visibility = Visibility.Visible;
            PlaceAtScreenPoint(scope, drone.AimScreenPoint);
        }
        deadZone.style.display = drone.IsAiming ? DisplayStyle.None : DisplayStyle.Flex;

        float diameter = 2f * ScreenToPanelLength(deadZone.panel, drone.DeadZoneRadius);
        deadZone.style.width = diameter;
        deadZone.style.height = diameter;
        autoCentre.text = drone.IsAutoCentring ? "AUTO-CENTRE ON" : "AUTO-CENTRE OFF";

        throttle.text = drone.IsAiming ? "AIM"
            : drone.IsBoosting ? "BOOST"
            : (drone.Throttle * 100f).ToString("F0", CultureInfo.InvariantCulture) + " %";
        // Invariant culture keeps a dot separator regardless of regional settings.
        speed.text = drone.GetSpeed().ToString("F1", CultureInfo.InvariantCulture) + " M/S";
        altitude.text = drone.GetAltitude().ToString("F1", CultureInfo.InvariantCulture) + " M";
        distance.text = drone.GetDistanceTravelled().ToString("F0", CultureInfo.InvariantCulture) + " M";
    }

    // A length in screen pixels as panel pixels (the panel may be scaled).
    private static float ScreenToPanelLength(IPanel panel, float length) =>
        RuntimePanelUtils.ScreenToPanel(panel, new Vector2(length, 0f)).x
        - RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero).x;

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
