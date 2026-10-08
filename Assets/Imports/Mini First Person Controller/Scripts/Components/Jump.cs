using UnityEngine;
using Zenject;

public class Jump : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float jumpStrength = 2;
    public event System.Action Jumped;

    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    GroundCheck groundCheck;

    [Inject] private InputService input;


    void Reset()
    {
        // Try to get groundCheck.
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Awake()
    {
        // Get rigidbody.
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        input.OnJump += TryJump;
    }

    void OnDisable()
    {
        input.OnJump -= TryJump;
    }

    void TryJump()
    {
        // Jump when the Jump button is pressed and we are on the ground.
        if (!groundCheck || groundCheck.isGrounded)
        {
            _rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
            Jumped?.Invoke();
        }
    }
}
