using UnityEngine;

// Floats a Rigidbody in any WaterVolume around its origin. Lift grows with how
// deep the origin is under the surface and equals gravity at floatDepth, so the
// body settles with its origin floatDepth under water; the water also damps its
// velocity, so it bobs a little and stops instead of springing forever.
[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    [Tooltip("Layers the water volumes live on")]
    [SerializeField] private LayerMask waterLayers = 1 << 2;
    [Tooltip("Depth of the origin under the surface at which the body floats still")]
    [SerializeField] private float floatDepth = 1.3f;
    [Tooltip("Velocity damping per second once the body is floatDepth deep")]
    [SerializeField] private float waterDrag = 4f;
    [Tooltip("Lift limit in g, so a body pushed deep comes up at a sane speed")]
    [SerializeField] private float maxLift = 2f;

    private readonly Collider[] hits = new Collider[4];
    private Rigidbody body;

    // How deep the origin is under the surface; 0 when dry.
    public float Depth { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Depth = MeasureDepth(body.position);
        if (Depth <= 0f)
            return;

        float submersion = Depth / floatDepth;
        float gravity = -Physics.gravity.y;
        body.AddForce(Vector3.up * (Mathf.Min(submersion, maxLift) * gravity), ForceMode.Acceleration);
        body.AddForce(-body.linearVelocity * (waterDrag * Mathf.Min(submersion, 1f)), ForceMode.Acceleration);
    }

    private float MeasureDepth(Vector3 point)
    {
        int count = Physics.OverlapSphereNonAlloc(point, 0.05f, hits, waterLayers, QueryTriggerInteraction.Collide);
        float depth = 0f;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].TryGetComponent(out WaterVolume water))
                depth = Mathf.Max(depth, water.SurfaceHeight - point.y);
        }
        return depth;
    }
}
