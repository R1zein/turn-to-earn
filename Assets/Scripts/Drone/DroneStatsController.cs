using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

// Telemetry readout for the drone: speed, altitude above world zero and the total
// distance flown.
//
// Sits at the top of the dependency chain: it reaches down into the drone scripts
// and drives the HUD, while nothing reaches back into it. It therefore asks the
// owning scripts for their numbers instead of collecting Rigidbody or transform
// state of its own - whoever owns the data stays the one that measures it.
public class DroneStatsController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private UIDocument hud;             // document hosting the DroneStats panel
    [SerializeField] private DroneControl droneControl;  // telemetry owner

    [Header("Element names")]
    [SerializeField] private string speedValueName = "SpeedValue";
    [SerializeField] private string altitudeValueName = "AltitudeValue";
    [SerializeField] private string distanceValueName = "DistanceValue";

    private Label speedValue;
    private Label altitudeValue;
    private Label distanceValue;
    private bool hudResolved;

    private void Update()
    {
        ResolveHud();

        if (droneControl == null)
            return;

        // Formatted with the invariant culture so the readout keeps a dot separator
        // regardless of the machine's regional settings.
        if (speedValue != null)
            speedValue.text = droneControl.GetSpeed().ToString("F1", CultureInfo.InvariantCulture) + " M/S";

        if (altitudeValue != null)
            altitudeValue.text = droneControl.GetAltitude().ToString("F1", CultureInfo.InvariantCulture) + " M";

        if (distanceValue != null)
            distanceValue.text = droneControl.GetDistanceTravelled().ToString("F0", CultureInfo.InvariantCulture) + " M";
    }

    // The document builds its elements on its own OnEnable, which may land after ours,
    // so grab them on the first Update that finds a live root.
    private void ResolveHud()
    {
        if (hudResolved || hud == null)
            return;

        VisualElement root = hud.rootVisualElement;
        if (root == null)
            return;

        speedValue = root.Q<Label>(speedValueName);
        altitudeValue = root.Q<Label>(altitudeValueName);
        distanceValue = root.Q<Label>(distanceValueName);
        hudResolved = true;
    }
}
