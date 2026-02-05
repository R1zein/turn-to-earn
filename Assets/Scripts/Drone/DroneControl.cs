using MagicPigGames;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class DroneControl : MonoBehaviour
{
    private float side;
    private float forward;
    private float vertical;
    private bool accelerate;
    public float deceleration;
    public float rotationSpeed;
    public float restoreSpeed;
    private Rigidbody rb;
    public float speed;
    private Vector3 movementDirection;

    public float maxTilt = 45f;
    public float yawRate = 30f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        ReadInput();
        Rotation();
        HorizontalMovement();
        RestoreRotation();
    }
    private void FixedUpdate()
    {
        Movement();
        VerticalMovement();

    }
    private void HorizontalMovement()
    {
        Vector3 euler = transform.eulerAngles;
        float currentPitch = GetSignedAngle(euler.x);
        float currentRoll = GetSignedAngle(euler.z);

        float pitchDelta = forward * rotationSpeed * Time.deltaTime;
        float rollDelta = -side * rotationSpeed * Time.deltaTime;

        float newPitch = currentPitch + pitchDelta;
        float newRoll = currentRoll + rollDelta;

        if (Mathf.Abs(newPitch) > maxTilt)
        {
            pitchDelta = Mathf.Clamp(newPitch, -maxTilt, maxTilt) - currentPitch;
        }

        if (Mathf.Abs(newRoll) > maxTilt)
        {
            rollDelta = Mathf.Clamp(newRoll, -maxTilt, maxTilt) - currentRoll;
        }

        transform.Rotate(pitchDelta, 0f, rollDelta, Space.Self);

    }
    private void VerticalMovement() 
    {
        Vector3 move = transform.up * vertical * speed;
        move *= Time.fixedDeltaTime;
        rb.linearVelocity += move;
    } 
    private void ReadInput()
    {
        side = Input.GetAxis("Horizontal");
        forward = Input.GetAxis("Vertical");
        accelerate = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKey(KeyCode.Space))
        {
            vertical = 1;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            vertical = -1;
        }
        else 
        {
            vertical = 0;
        }
    }
    private void Movement()
    {
        float roll = GetSignedAngle(transform.eulerAngles.z);
        movementDirection = Quaternion.Euler(0f, -roll / 2f, 0f) * transform. forward;

        Vector3 move = movementDirection * speed*(accelerate ? 1 : 0);
        move *= Time.fixedDeltaTime;
        rb.linearVelocity += move;
        var delta = 1 - deceleration * Time.fixedDeltaTime;
        rb.linearVelocity *= delta;
    }
    private void Rotation()
    {
        if (accelerate == false)
        {
            return;
        }

        float roll = GetSignedAngle(transform. eulerAngles.z);
        float pitch = GetSignedAngle(transform.eulerAngles.x);

        float cosValue = Mathf.Abs(Mathf.Cos(pitch * Mathf.Deg2Rad));
        float yawDelta = -(roll / maxTilt) * yawRate * cosValue * cosValue * Time.deltaTime;

        transform.Rotate(0f, yawDelta, 0f, Space.World);

    }

    private void RestoreRotation()
    {

        var z = GetSignedAngle(transform.eulerAngles.z);
        var angles = transform.eulerAngles;
        var delta = restoreSpeed * Time.deltaTime;
        if (Mathf.Abs(z) < 1 && side == 0)
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            z = 0;
        }
        if(z!=0)
        {
            if (z < 0f)
            {
                transform.Rotate(0, 0, delta, Space.Self);
            }
            else
            {
                transform.Rotate(0, 0, -delta, Space.Self);
            }
        }

    }

    private float GetSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
