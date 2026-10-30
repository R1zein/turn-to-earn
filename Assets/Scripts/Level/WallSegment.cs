using UnityEngine;
using Zenject;

// A wall module. Its palisade runs along the prefab's X through the pivot; the
// walkway is on the +Z side. WallCorners joins placed segments that meet at a corner.
public class WallSegment : MonoBehaviour
{
    public const float HalfLength = 2f;

    [Inject] private WallCorners corners;

    public Vector3 Along => transform.right;

    private void OnDestroy()
    {
        corners.Remove(this);
    }
}
