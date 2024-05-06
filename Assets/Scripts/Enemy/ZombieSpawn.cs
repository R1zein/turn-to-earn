using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawn : MonoBehaviour
{
    [SerializeField] private GameObject zombie;
    [SerializeField] private float minSpawnTime;
    [SerializeField] private float maxSpawnTime;
    [SerializeField] private float spawnRate;
    [SerializeField] private Transform spawnPos;
    private float timer;


    private void Update()
    {
        timer += Time.deltaTime;
        if(timer >= spawnRate)
        {
            timer = 0;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Instantiate(zombie, spawnPos.position, Quaternion.identity);
    }
}
