using UnityEngine;

public class Building : MonoBehaviour
{
    private StatsHandler statsHandler;
    public AudioClip[] audioClips;
    private AudioSource audioSource;
    private void Awake()
    {
        statsHandler = GetComponent<StatsHandler>();
        audioSource = GetComponent<AudioSource>();
    }
    private void OnEnable()
    {
        statsHandler.OnDeath += Death;
        statsHandler.OnDamage += TakeDamage;
    }

    private void Death()
    {
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        statsHandler.OnDeath -= Death;
        statsHandler.OnDamage -= TakeDamage;
    }

    private void TakeDamage()
    {
        int randomIndex = Random.Range(0,audioClips.Length);
        audioSource.PlayOneShot(audioClips[randomIndex]);
    }
}
