using System.Collections.Generic;
using UnityEngine;

// A shooting range: its children are the spots where target dummies stand,
// facing the way each child faces. References only, no logic -
// ShootingRangeService fills the spots.
public class ShootingRange : MonoBehaviour
{
    private readonly List<Transform> spots = new List<Transform>();

    public IReadOnlyList<Transform> Spots => spots;

    private void Awake()
    {
        foreach (Transform child in transform)
            spots.Add(child);
    }
}
