using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public abstract class ResourceController : MonoBehaviour
{
    [SerializeField] private ParticleSystem destroyEffect;

    [Inject] protected ResourceWallet wallet;
    [Inject] private ResourceSpawner spawner;

    private MeshCollider meshColliderStone;
    private MeshRenderer meshRendererStone;
    [HideInInspector] public int positionID;
    public MineableResourses resourse;
    private bool isDying = false;
    [SerializeField] protected int resourceStore;
    [SerializeField] protected int oneHitResource;

    private void Start()
    {
        meshColliderStone = GetComponent<MeshCollider>();
        meshRendererStone = GetComponent<MeshRenderer>();
    }

    private async Awaitable DeathEffect()
    {
        meshColliderStone.enabled = false;
        meshRendererStone.enabled = false;
        Instantiate(destroyEffect, transform.position, Quaternion.identity);
        await Awaitable.WaitForSecondsAsync(2f);
        spawner.FindDestroyed(positionID);
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
