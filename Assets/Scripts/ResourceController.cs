using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ResourceController : MonoBehaviour
{
    [SerializeField] private ParticleSystem destroyEffect;

    private MeshCollider meshColliderStone;
    private MeshRenderer meshRendererStone;
    private StoneSpawner spawner;
    [HideInInspector] public int positionID;
    public MineableResourses resourse;
    private bool isDying = false;
    [SerializeField] protected int resourceStore;
    [SerializeField] protected int oneHitResource;

    private void Start()
    {
        meshColliderStone = GetComponent<MeshCollider>();
        meshRendererStone = GetComponent<MeshRenderer>();
        spawner = FindAnyObjectByType<StoneSpawner>();
    }

    //проверяет остаток руды                                   

    private IEnumerator DeathEffect()
    {
        meshColliderStone.enabled = false;
        meshRendererStone.enabled = false;
        Instantiate(destroyEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(2f);
        spawner.FindDestroyed(positionID);
        Destroy(gameObject);
    }
    protected void Death()
    {
        if(isDying == false)
        {
            StartCoroutine(DeathEffect());
            isDying = true;
        }
    }

    public abstract void TakeHit();

}
