using UnityEngine;

[CreateAssetMenu(menuName = "Turn To Earn/Target Type")]
public class TargetType : ScriptableObject
{
    [SerializeField] private string displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}
