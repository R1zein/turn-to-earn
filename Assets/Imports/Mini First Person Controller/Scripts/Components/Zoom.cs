using UnityEngine;

[ExecuteInEditMode]
public class Zoom : MonoBehaviour
{
    private Camera _camera;
    public float defaultFOV = 60;
    public float maxZoomFOV = 15;
    [Range(0, 1)]
    public float currentZoom;
    public float sensitivity = 1;


    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera)
        {
            defaultFOV = _camera.fieldOfView;
        }
    }

    void Update()
    {
        currentZoom += Input.mouseScrollDelta.y * sensitivity * .05f;
        currentZoom = Mathf.Clamp01(currentZoom);
        _camera.fieldOfView = Mathf.Lerp(defaultFOV, maxZoomFOV, currentZoom);
    }
}
