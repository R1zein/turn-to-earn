using UnityEngine;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [SerializeField] private TargetType type;
    [SerializeField] private float weightOffset;
    [SerializeField, Min(1)] private int capacity = 4;

    private TargetGeometry geometry;

    public TargetType Type => type;
    public float WeightOffset => weightOffset;
    public int Capacity => Mathf.Max(1, capacity);
    public StatsHandler Health { get; private set; }
    public Fraction Faction => Health != null ? Health.fraction : Fraction.Neutral;
    public Vector3 AimPoint => geometry.AimPoint;

    private void Awake()
    {
        Health = GetComponent<StatsHandler>();
        geometry = new TargetGeometry(transform);
    }

    public Vector3 ClosestPoint(Vector3 from) => geometry.ClosestPoint(from);
}
