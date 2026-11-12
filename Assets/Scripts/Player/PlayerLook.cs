using UnityEngine;
using Zenject;

// Mouse look, on the camera. The camera takes its whole world rotation from here
// every frame; the body under it is turned to the same yaw by PlayerMovement at
// the physics rate. The body's Transform is never written: that would knock an
// interpolated Rigidbody back to its last physics pose and make walking stutter.
//
// Sensitivity and smoothing are the hand-tuned values of the old controller and
// the angle math is the same — only who turns the body has changed.
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float smoothing = 1.5f;

    [Inject] private InputService input;

    // x — yaw, y — pitch (up is positive), degrees.
    private Vector2 angles;
    private Vector2 frameVelocity;

    public float Yaw => angles.x;
    public float Pitch => angles.y;

    private void Awake()
    {
        angles.x = transform.eulerAngles.y;
    }

    private void Update()
    {
        Vector2 rawFrameVelocity = input.Look * sensitivity;
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1f / smoothing);
        angles += frameVelocity;
        angles.y = Mathf.Clamp(angles.y, -90f, 90f);

        transform.rotation = Quaternion.Euler(-angles.y, angles.x, 0f);
    }
}
