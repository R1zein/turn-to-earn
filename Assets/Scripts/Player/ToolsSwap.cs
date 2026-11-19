using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

// The tool in hand: the number keys pick from `tools` in order, 1 is the first.
// A key past the end of the list does nothing.
public class ToolsSwap : MonoBehaviour
{
    public GameObject[] tools;
    private int toolIndex;
    private Camera worldCamera;
    private Camera toolCamera;

    [Inject] private InputService input;

    private void Start()
    {
        worldCamera = GetComponent<Camera>();
        int viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
        CreateToolCamera(1 << viewmodelLayer);
        foreach (GameObject tool in tools)
        {
            foreach (Renderer part in tool.GetComponentsInChildren<Renderer>(true))
            {
                part.gameObject.layer = viewmodelLayer;
                part.shadowCastingMode = ShadowCastingMode.Off;
            }
            tool.SetActive(false);
        }
        Equip();
    }

    private void CreateToolCamera(int toolLayerMask)
    {
        var cameraObject = new GameObject("Viewmodel Camera");
        cameraObject.transform.SetParent(transform, false);
        toolCamera = cameraObject.AddComponent<Camera>();
        toolCamera.CopyFrom(worldCamera);
        toolCamera.cullingMask = toolLayerMask;
        toolCamera.nearClipPlane = 0.02f;
        toolCamera.farClipPlane = 10f;

        UniversalAdditionalCameraData overlay = toolCamera.GetUniversalAdditionalCameraData();
        overlay.renderType = CameraRenderType.Overlay;
        worldCamera.cullingMask &= ~toolLayerMask;
        worldCamera.GetUniversalAdditionalCameraData().cameraStack.Add(toolCamera);
    }

    private void OnEnable()
    {
        input.OnToolSlot += Select;
    }

    private void OnDisable()
    {
        input.OnToolSlot -= Select;
    }

    private void Select(int slot)
    {
        if (slot < 0 || slot >= tools.Length || toolIndex == slot)
            return;

        tools[toolIndex].SetActive(false);
        toolIndex = slot;
        Equip();
    }

    private void Equip()
    {
        GameObject tool = tools[toolIndex];
        tool.SetActive(true);
        if (tool.TryGetComponent(out Animator animator))
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void OnDestroy()
    {
        if (toolCamera != null && worldCamera != null)
            worldCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(toolCamera);
    }
}
