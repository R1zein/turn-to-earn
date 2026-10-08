using UnityEngine;
using Zenject;

public class Building : MonoBehaviour
{
    [Inject] private TargetRegistry registry;

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
        registry.Add(TargetKind.Building, this);
    }

    private void Death()
    {
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        registry.Remove(TargetKind.Building, this);
        statsHandler.OnDeath -= Death;
        statsHandler.OnDamage -= TakeDamage;
    }

    private void TakeDamage()
    {
        int randomIndex = Random.Range(0,audioClips.Length);
        audioSource.PlayOneShot(audioClips[randomIndex]);
    }
}
