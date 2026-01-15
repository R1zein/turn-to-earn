using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    private Camera camera;
    public float distance;
    public float height;


    private void Awake()
    {
        camera = Camera.main;
    }
    private void LateUpdate()
    {
        Vector3 targetPosition = transform.position - transform.forward * distance + transform.up * height;
        camera.transform.position = targetPosition;
        camera.transform.LookAt(transform.position, Vector3.up);
    }
}
