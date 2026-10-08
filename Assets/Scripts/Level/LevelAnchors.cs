using UnityEngine;

// Map of the level: scene objects and points that services need.
// References only, no logic.
public class LevelAnchors : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private ReflectionProbe reflectionProbe;

    public Light Sun => sun;
    public ReflectionProbe ReflectionProbe => reflectionProbe;
}
