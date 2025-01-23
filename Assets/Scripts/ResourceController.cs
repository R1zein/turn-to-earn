using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ResourceController : MonoBehaviour
{
    [SerializeField] private ParticleSystem destroyEffect;

    private MeshCollider meshColliderStone;
    private MeshRenderer meshRendererStone;
    private ResourceSpawner spawner;
    [HideInInspector] public int positionID;
    public MineableResourses resourse;
    private bool isDying = false;
    [SerializeField] protected int resourceStore;
    [SerializeField] protected int oneHitResource;

    private void Start()
    {
        meshColliderStone = GetComponent<MeshCollider>();
        meshRendererStone = GetComponent<MeshRenderer>();
        spawner = FindAnyObjectByType<ResourceSpawner>();
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
