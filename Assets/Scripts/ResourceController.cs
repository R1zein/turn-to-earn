using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public abstract class ResourceController : MonoBehaviour
{
    [SerializeField] private ParticleSystem destroyEffect;

    [Inject] protected ResourceWallet wallet;
    [Inject] private ResourceNodeSpawner spawner;
    [Inject] private TargetRegistry registry;

    private MeshCollider meshColliderStone;
    private MeshRenderer meshRendererStone;
    public MineableResourses resourse;
    private bool isDying = false;
    [SerializeField] protected int resourceStore;
    [SerializeField] protected int oneHitResource;

    private void Start()
    {
        meshColliderStone = GetComponent<MeshCollider>();
        meshRendererStone = GetComponent<MeshRenderer>();
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
        meshColliderStone.enabled = false;
        meshRendererStone.enabled = false;
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
