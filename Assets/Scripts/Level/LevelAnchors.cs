using System.Collections.Generic;
using UnityEngine;

// Map of the level: scene objects and points that services need.
// References only, no logic.
public class LevelAnchors : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private ReflectionProbe reflectionProbe;
    [SerializeField] private Portal[] portals;
    [SerializeField] private ResourceArea[] resourceAreas;

    public Light Sun => sun;
    public ReflectionProbe ReflectionProbe => reflectionProbe;
    public IReadOnlyList<Portal> Portals => portals;
    public IReadOnlyList<ResourceArea> ResourceAreas => resourceAreas;
}
