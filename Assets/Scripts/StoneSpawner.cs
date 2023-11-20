using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StoneSpawner : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private List<GameObject> stonePrefabs = new List<GameObject>();
    [SerializeField] private int length;
    private int value;
    [SerializeField] private int timer;


    private Dictionary<int, int> positions = new Dictionary<int, int>(); 


    private void Start()
    {
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            positions.Add(i, 0);
        }
        StartCoroutine(SpawnObjects());
    }
    private IEnumerator SpawnObjects()
    {
        while (true)
        {
            yield return new WaitForSeconds(timer);
            var point = Random.Range(0, spawnPoints.Count);
            positions.TryGetValue(point, out value);
            if (value == 0)
            {
                var stone = Instantiate(stonePrefabs[Random.Range(0, stonePrefabs.Count)], spawnPoints[point].position, Quaternion.identity);
                positions[point] = 1;
                stone.GetComponent<StoneController>().positionID = point;
            }
        }
    }

    public void FindDestroyed(int point)
    {
        positions.TryGetValue(point, out value);
        if (value == 1)
        {
            positions[point] = 0;
        }
    }
}
