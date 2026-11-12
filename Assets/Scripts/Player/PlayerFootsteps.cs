using UnityEngine;

// Sound of the legs: the step loop while moving on the ground, a thud on landing.
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private AudioSource stepAudio;
    [SerializeField] private AudioSource landingAudio;
    [SerializeField] private AudioClip[] landingClips;
    [Tooltip("Slower than this the steps are silent, m/s")]
    [SerializeField] private float minSpeed = 0.5f;

    private PlayerMovement movement;

    private void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void OnEnable()
    {
        movement.Landed += PlayLanding;
    }

    private void OnDisable()
    {
        movement.Landed -= PlayLanding;
    }

    private void Update()
    {
        bool isWalking = movement.IsGrounded && movement.PlanarSpeed >= minSpeed;
        if (!isWalking)
            stepAudio.Pause();
        else if (!stepAudio.isPlaying)
            stepAudio.Play();
    }

    private void PlayLanding()
    {
        landingAudio.clip = landingClips[Random.Range(0, landingClips.Length)];
        landingAudio.Play();
    }
}
