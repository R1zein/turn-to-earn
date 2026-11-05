using UnityEngine;
using Zenject;

// Which structure a hit is on, as the player sees it: what the structure panel shows
// and what the hammer mends. A wall corner is not a structure to the player but the
// ends of two walls, so a hit on it gives the wall whose end was hit.
public class StructureLookup
{
    [Inject] private WallCorners corners;

    public Structure At(RaycastHit hit)
    {
        WallSegment wall = corners.WallAt(hit);
        return wall != null ? wall.GetComponent<Structure>() : hit.collider.GetComponentInParent<Structure>();
    }
}
