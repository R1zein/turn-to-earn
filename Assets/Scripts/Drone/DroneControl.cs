using MagicPigGames;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DroneControl : MonoBehaviour
{
    private float side;
    private float forward;
    private float vertical;
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
    }
    private void FixedUpdate()
    {
        Movement();
    }
    private void HorizontalMovement()
    {

    }
    private void VerticalMovement() 
    {

    } 
    private void ReadInput()
    {
        side = Input.GetAxis("Horizontal");
        forward = Input.GetAxis("Vertical");
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
        Vector3 move = transform.forward * forward + transform.right * side + transform.up * vertical;
        move *= Time.fixedDeltaTime;
        //rb.MovePosition(transform.position + move*speed);
        rb.linearVelocity += move*speed;
    }
    private void Rotation()
    {
        //transform.rotation=Camera.main.transform.rotation;
    }
}
