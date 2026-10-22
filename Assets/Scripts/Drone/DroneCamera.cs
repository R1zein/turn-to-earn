using UnityEngine;
using Zenject;

// Chase camera fixed behind the drone's HEADING with the horizon kept level and a
// slight look down, so the drone sits just below the screen centre and rolls and
// banks are watched from outside instead of spinning the view. The heading is
// followed while the nose is off the vertical; through a vertical climb it is
// held, or the view would whip round as the nose passes the zenith.
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

    private float heading;   // yaw, degrees

    private void Start()
    {
        heading = transform.eulerAngles.y;
        camera.transform.SetPositionAndRotation(TargetPosition(), ViewRotation());
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

        camera.transform.position = Vector3.Lerp(camera.transform.position, TargetPosition(),
            1f - Mathf.Exp(-followSharpness * dt));
        camera.transform.rotation = ViewRotation();
    }

    private Vector3 TargetPosition() =>
        transform.position - Quaternion.Euler(0f, heading, 0f) * Vector3.forward * distance + Vector3.up * height;

    private Quaternion ViewRotation() => Quaternion.Euler(pitchDown, heading, 0f);
}
