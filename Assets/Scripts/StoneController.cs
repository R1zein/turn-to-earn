using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneController : MonoBehaviour
{
    //сколько руды в камне
    [SerializeField] int oreStore;
    //руда за один удар
    [SerializeField] int oneHitOre;

    [SerializeField] private ParticleSystem destroyStoneEffect;

    private MeshCollider meshColliderStone;
    private MeshRenderer meshRendererStone;
    private StoneSpawner spawner;
    public int positionID;


    private void Start()
    {
        meshColliderStone = GetComponent<MeshCollider>();      
        meshRendererStone = GetComponent<MeshRenderer>();      
        spawner = FindObjectOfType<StoneSpawner>();            
    }                                                          
                                                               
    //проверяет остаток руды                                   
    public void TakeHit()                                      
    {                                                          
        oreStore -= oneHitOre;                                 
        Player.playerInstance.AddPlayerOre(oneHitOre);         
        if(oreStore<=0)                                        
        {                                                      
            StartCoroutine(DeathEffect());                     
            //добавить эффект разрушения камня                 
        }                                                      
    }                                                          
    private IEnumerator DeathEffect()                          
    {                                                          
        meshColliderStone.enabled = false;                     
        meshRendererStone.enabled = false;                     
        destroyStoneEffect.Play();                             
        yield return new WaitForSeconds(2f);
        spawner.FindDestroyed(positionID);
        Destroy(gameObject);                                   
    }                                                          
}                                                              
                                                               