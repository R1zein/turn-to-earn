using UnityEngine;

public class Player : MonoBehaviour
{
    private StatsHandler statsHandler;
    public Camera deathCamera;


    void Awake()
    {
        statsHandler = GetComponent<StatsHandler>();
    }
    private void Death()
    {
        var camera = Instantiate(deathCamera, transform.position, transform.rotation);
        var position = camera.transform.position;
        position.y += 5;
        position.z -= 2.5f;
        camera.transform.Rotate(70, 0, 0);
        camera.transform.position = position;
        Destroy(gameObject);
    }
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
    }

    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
    }
}
