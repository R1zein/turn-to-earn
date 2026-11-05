using System.Collections.Generic;
using UnityEngine;

// A wall corner model: the parts of the two walls it replaces, as boxes in the
// model's own frame (the through wall's frame shifted to the corner, mirrored with
// the model at the -X end). WallCorners hands them to WallMeshCuts.
public class WallCornerModel : MonoBehaviour
{
    [Tooltip("Triangles of either wall lying wholly inside one of these boxes are hidden and stop colliding")]
    [SerializeField] private Bounds[] cuts;

    public IReadOnlyList<Bounds> Cuts => cuts;
}
