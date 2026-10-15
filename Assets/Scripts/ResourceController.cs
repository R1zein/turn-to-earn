using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public abstract class ResourceController : MonoBehaviour
{
    [SerializeField] private ParticleSystem destroyEffect;

    [Inject] protected ResourceWallet wallet;
    [Inject] private ResourceNodeSpawner spawner;
    [Inject] private TargetRegistry registry;

    // Any collider/renderer: ore veins use a SphereCollider, and looking for a
    // MeshCollider left them null - a mined-out vein threw on death and never vanished.
    private Collider nodeCollider;
    private Renderer nodeRenderer;
    [FormerlySerializedAs("resourse")] public MineableResource resource;
    private bool isDying = false;
    [SerializeField] protected int resourceStore;
    [SerializeField] protected int oneHitResource;

    private void Start()
    {
        nodeCollider = GetComponent<Collider>();
        nodeRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        registry.Add(TargetKind.ResourceNode, this);
    }

    private void OnDisable()
    {
        registry.Remove(TargetKind.ResourceNode, this);
    }

    private async Awaitable DeathEffect()
    {
        // Mined out: bots stop walking to it while the effect plays.
        registry.Remove(TargetKind.ResourceNode, this);
        nodeCollider.enabled = false;
        nodeRenderer.enabled = false;
        Instantiate(destroyEffect, transform.position, Quaternion.identity);
        await Awaitable.WaitForSecondsAsync(2f);
        spawner.Release(this);
        Destroy(gameObject);
    }
    protected void Death()
    {
        if(isDying == false)
        {
            DeathEffect();
            isDying = true;
        }
    }

    public abstract void TakeHit();

}
