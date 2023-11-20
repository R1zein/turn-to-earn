using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    [SerializeField] private List<AudioClip> miningSounds = new List<AudioClip>();
    private AudioSource audioSource;
    

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void PlayMiningSound()
    {
        var random = Random.Range(0, miningSounds.Count);
        audioSource.clip = miningSounds[random];
        audioSource.Play();
    }
}
