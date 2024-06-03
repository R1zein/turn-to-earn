using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class ZombieSpawn : MonoBehaviour
{
    [SerializeField] private GameObject zombie;
    [SerializeField] private float minSpawnTime;
    [SerializeField] private float maxSpawnTime;
    [SerializeField] private float spawnRate;
    [SerializeField] private Transform spawnPos;
    private Animator animator;
    private float timer;
    private float spawnTime;
    [SerializeField] private NavMeshSurface surface;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
    }

    private void SpawnEnemy()
    {
        Instantiate(zombie, spawnPos.position, Quaternion.identity);
        
    }
    public void OpenPortal()
    {
        animator.Play("OpenPortal");
        spawnTime = Random.Range(minSpawnTime, maxSpawnTime);
        timer = 0;
        StartCoroutine(StartSpawning());
    }
    private void ClosePortal()
    {
        animator.Play("ClosePortal");
        timer = 0;
    }
    private IEnumerator StartSpawning()
    {
        yield return null;
        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        surface.BuildNavMesh();
        while (timer < spawnTime)
        {
            yield return new WaitForSeconds(spawnRate);
            SpawnEnemy();
        }
        ClosePortal();
        
    }
}
