using UnityEngine;

[DisallowMultipleComponent]
public class TargetSeeker : MonoBehaviour
{
    [SerializeField] private TargetProfile profile;

    public TargetProfile Profile => profile;
}
