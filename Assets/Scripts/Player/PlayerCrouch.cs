using System;
using UnityEngine;
using Zenject;

// Crouching while the key is held: the head and the top of the capsule come down
// by the same amount and the legs are limited to crouchSpeed.
[RequireComponent(typeof(PlayerMovement), typeof(CapsuleCollider))]
public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] private float crouchSpeed = 2f;
    [Tooltip("Local height of the camera while crouched")]
    [SerializeField] private float crouchHeadHeight = 0.5f;

    [Inject] private InputService input;

    private PlayerMovement movement;
    private CapsuleCollider capsule;
    private Transform head;
    private Func<float> speedLimit;
    private float standHeadHeight;
    private float standCapsuleHeight;

    public bool IsCrouched { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        capsule = GetComponent<CapsuleCollider>();
        head = GetComponentInChildren<PlayerLook>().transform;
        speedLimit = () => crouchSpeed;
        standHeadHeight = head.localPosition.y;
        standCapsuleHeight = capsule.height;
    }

    private void Update()
    {
        if (input.IsCrouchHeld != IsCrouched)
            SetCrouched(input.IsCrouchHeld);
    }

    private void OnDisable()
    {
        if (IsCrouched)
            SetCrouched(false);
    }

    private void SetCrouched(bool value)
    {
        IsCrouched = value;

        float headHeight = value ? crouchHeadHeight : standHeadHeight;
        Vector3 headPosition = head.localPosition;
        headPosition.y = headHeight;
        head.localPosition = headPosition;

        // The feet stay where they are: the capsule shrinks from the top.
        capsule.height = standCapsuleHeight - (standHeadHeight - headHeight);
        capsule.center = Vector3.up * (capsule.height * 0.5f);

        if (value)
            movement.AddSpeedLimit(speedLimit);
        else
            movement.RemoveSpeedLimit(speedLimit);
    }
}
