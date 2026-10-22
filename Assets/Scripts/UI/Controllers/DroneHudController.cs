using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Drone HUD: the cyan ring sits exactly on the invisible steering cursor; the pink
// diamond stays static in the screen centre (it lives in the UXML, code never
// moves it). While aiming the ring marks where the mouse has pointed the aim (the
// nose snaps there). The telemetry panel shows throttle, speed, altitude above
// world zero and total distance flown - numbers the drone measures itself - and
// whether a bomb is ready or how long until it is.
public class DroneHudController : IInitializable, ITickable
{
    [Inject] private UIDocument document;
    [Inject] private DroneControl drone;
    [Inject] private DroneBombBay bombBay;
    [Inject] private Camera camera;

    // How far ahead the aim direction is projected while aiming, metres.
    private const float AimMarkerDistance = 100f;

    private VisualElement scope;
    private Label throttle, speed, altitude, distance, bomb;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        scope = root.Q("Scope");
        throttle = root.Q<Label>("ThrottleValue");
        speed = root.Q<Label>("SpeedValue");
        altitude = root.Q<Label>("AltitudeValue");
        distance = root.Q<Label>("DistanceValue");
        bomb = root.Q<Label>("BombValue");
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

        throttle.text = drone.IsAiming ? "AIM"
            : drone.IsBoosting ? "BOOST"
            : (drone.Throttle * 100f).ToString("F0", CultureInfo.InvariantCulture) + " %";
        // Invariant culture keeps a dot separator regardless of regional settings.
        speed.text = drone.GetSpeed().ToString("F1", CultureInfo.InvariantCulture) + " M/S";
        altitude.text = drone.GetAltitude().ToString("F1", CultureInfo.InvariantCulture) + " M";
        distance.text = drone.GetDistanceTravelled().ToString("F0", CultureInfo.InvariantCulture) + " M";
        float cooldown = bombBay.CooldownLeft;
        bomb.text = cooldown <= 0f ? "READY" : Mathf.CeilToInt(cooldown).ToString(CultureInfo.InvariantCulture) + " S";
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
