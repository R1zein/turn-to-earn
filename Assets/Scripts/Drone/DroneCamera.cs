using UnityEngine;
using Zenject;

// Chase camera: behind and above the drone's HEADING with the horizon kept level,
// so rolls and flips are watched from outside instead of spinning the view.
// The heading is followed only while the drone is roughly upright; mid-flip it
// is held, or the camera would swing round as the nose passes vertical.
public class DroneCamera : MonoBehaviour
{
    // A drone that is not at least this upright (up.y) is mid-trick.
    private const float UprightForHeading = 0.5f;

    [Inject] private Camera camera;

    public float distance = 6f;
    public float height = 2f;
    [Tooltip("1/s: how tightly the camera position follows; lower = more lag at speed")]
    [SerializeField] private float followSharpness = 10f;
    [Tooltip("1/s: how fast the camera swings round behind a new heading")]
    [SerializeField] private float headingSharpness = 5f;

    private float heading;   // yaw, degrees

    private void Start()
    {
        heading = transform.eulerAngles.y;
        camera.transform.position = TargetPosition();
        Aim();
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 forward = transform.forward;
        if (transform.up.y > UprightForHeading && forward.x * forward.x + forward.z * forward.z > 0.01f)
        {
            float target = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            heading = Mathf.LerpAngle(heading, target, 1f - Mathf.Exp(-headingSharpness * dt));
        }

        camera.transform.position = Vector3.Lerp(camera.transform.position, TargetPosition(),
            1f - Mathf.Exp(-followSharpness * dt));
        Aim();
    }

    private Vector3 TargetPosition() =>
        transform.position - Quaternion.Euler(0f, heading, 0f) * Vector3.forward * distance + Vector3.up * height;

    private void Aim() =>
        camera.transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
}
