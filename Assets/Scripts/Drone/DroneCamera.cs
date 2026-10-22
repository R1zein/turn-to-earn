using UnityEngine;
using Zenject;

// Two views, blended smoothly while the drone's Aim is held or released:
//  - CHASE: fixed behind the drone's HEADING with the horizon kept level and a
//    slight look down, so the drone sits just below the screen centre and rolls
//    and banks are watched from outside. The heading is followed while the nose
//    is off the vertical; through a vertical climb it is held, or the view would
//    whip round as the nose passes the zenith.
//  - FIRST PERSON (Aim held): at the nose, turning with the drone on every axis -
//    it pitches (X) and rolls (Z) with the body.
// The chase pose keeps tracking while aiming, so releasing Aim returns to a view
// that is already in place.
[RequireComponent(typeof(DroneControl))]
public class DroneCamera : MonoBehaviour
{
    [Inject] private Camera camera;

    public float distance = 8f;
    public float height = 2.5f;
    [Tooltip("Look down from the horizon, deg: the drone sits this much below the centre")]
    [SerializeField] private float pitchDown = 8f;
    [Tooltip("1/s: how fast the camera swings round behind a new heading")]
    [SerializeField] private float headingSharpness = 5f;
    [Tooltip("1/s: how tightly the camera position follows; lower = more lag at speed")]
    [SerializeField] private float followSharpness = 10f;

    [Header("First person")]
    [Tooltip("Camera position in the drone's own frame: just ahead of the nose")]
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.4f, 2.2f);
    [Tooltip("Seconds to switch between the views")]
    [SerializeField] private float switchTime = 0.25f;

    private DroneControl drone;
    private float heading;   // yaw, degrees
    private Vector3 chasePosition;
    private float firstPerson;   // 0 = chase, 1 = first person

    private void Start()
    {
        drone = GetComponent<DroneControl>();
        heading = transform.eulerAngles.y;
        chasePosition = ChaseTarget();
        camera.transform.SetPositionAndRotation(chasePosition, ChaseRotation());
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.y) < 0.9f)
        {
            float target = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            heading = Mathf.LerpAngle(heading, target, 1f - Mathf.Exp(-headingSharpness * dt));
        }
        chasePosition = Vector3.Lerp(chasePosition, ChaseTarget(), 1f - Mathf.Exp(-followSharpness * dt));

        firstPerson = Mathf.MoveTowards(firstPerson, drone.IsAiming ? 1f : 0f, dt / switchTime);
        float blend = Mathf.SmoothStep(0f, 1f, firstPerson);

        camera.transform.position = Vector3.Lerp(chasePosition, transform.TransformPoint(firstPersonOffset), blend);
        camera.transform.rotation = Quaternion.Slerp(ChaseRotation(), transform.rotation, blend);
    }

    private Vector3 ChaseTarget() =>
        transform.position - Quaternion.Euler(0f, heading, 0f) * Vector3.forward * distance + Vector3.up * height;

    private Quaternion ChaseRotation() => Quaternion.Euler(pitchDown, heading, 0f);
}
