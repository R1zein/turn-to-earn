using UnityEngine;
using UnityEngine.UIElements;

// Templates for UI elements created at runtime. Layout lives in the UXML; code only
// clones a template and fills in data.
[CreateAssetMenu(menuName = "Data/UI Template")]
public class UITemplate : ScriptableObject
{
    [SerializeField] private VisualTreeAsset itemCard;
    [SerializeField] private VisualTreeAsset healthMarker;

    public VisualTreeAsset ItemCard => itemCard;
    public VisualTreeAsset HealthMarker => healthMarker;
}
