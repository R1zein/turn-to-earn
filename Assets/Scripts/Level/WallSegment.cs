using UnityEngine;
using Zenject;

// A wall module. Its palisade runs along the prefab's X through the pivot; the
// walkway is on the +Z side. WallCorners joins placed segments that meet at a corner,
// WallStairs those standing in one line at different heights.
public class WallSegment : MonoBehaviour
{
    public const float HalfLength = 2f;

    [Inject] private WallCorners corners;
    [Inject] private WallStairs stairs;
    [Inject] private WallMeshCuts cuts;

    public Vector3 Along => transform.right;

    public GridFootprint Cells => GetComponent<GridBuilding>().Footprint;

    private bool isJoined;

    public void Join()
    {
        isJoined = true;
        corners.Add(this);
        stairs.Add(this);
    }

    private void OnDisable()
    {
        if (!isJoined)
            return;

        isJoined = false;
        corners.Remove(this);
        stairs.Remove(this);
        cuts.Forget(this);
    }
}
