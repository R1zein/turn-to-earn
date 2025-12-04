using MagicPigGames;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class DroneControl : MonoBehaviour
{
    private float side;
    private float forward;
    private float vertical;
    private bool accelerate;
    public float deceleration;
    public float rotationSpeed;
    private Rigidbody rb;
    public float speed;

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
    }
    private void FixedUpdate()
    {
        Movement();
        VerticalMovement();

    }
    private void HorizontalMovement()
    {
        transform.Rotate(forward * rotationSpeed * Time.deltaTime, 0, -side * rotationSpeed * Time.deltaTime, Space.Self);
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
        Vector3 move = transform.forward * speed*(accelerate ? 1 : 0);
        move *= Time.fixedDeltaTime;
        rb.linearVelocity += move;
        var delta = 1 - deceleration * Time.fixedDeltaTime;
        rb.linearVelocity *= delta;
    }
    private void Rotation()
    {
        
    }
}
