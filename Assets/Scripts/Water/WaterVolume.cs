using UnityEngine;

// A body of still water: a trigger box whose top face is the surface. The visible
// water is a Bitgem WaterVolumeBox on the same object, sized to the same box.
// Lives on the Ignore Raycast layer, so tool and build raycasts pass through it.
[RequireComponent(typeof(BoxCollider))]
public class WaterVolume : MonoBehaviour
{
    private BoxCollider box;

    public float SurfaceHeight => box.bounds.max.y;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
    }
}
