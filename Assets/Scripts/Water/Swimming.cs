using System;
using UnityEngine;
using Zenject;

// Tells the legs they are in water, and limits them to GameConfig.SwimSpeed once
// it is deeper than swimDepth — a speed limit of PlayerMovement, as crouching is.
// Staying afloat is Buoyancy's job.
[RequireComponent(typeof(Buoyancy), typeof(PlayerMovement))]
public class Swimming : MonoBehaviour
{
    [Inject] private GameConfig config;

    [Tooltip("Depth of the feet under the surface from which the player swims instead of wading")]
    [SerializeField] private float swimDepth = 0.9f;

    private Buoyancy buoyancy;
    private PlayerMovement movement;
    private Func<float> swimSpeed;

    public bool IsSwimming { get; private set; }

    private void Awake()
    {
        buoyancy = GetComponent<Buoyancy>();
        movement = GetComponent<PlayerMovement>();
        swimSpeed = () => config.SwimSpeed;
    }

    private void FixedUpdate()
    {
        movement.IsInWater = buoyancy.Depth > 0f;

        bool isDeep = buoyancy.Depth > swimDepth;
        if (isDeep != IsSwimming)
            SetSwimming(isDeep);
    }

    private void OnDisable()
    {
        movement.IsInWater = false;
        if (IsSwimming)
            SetSwimming(false);
    }

    private void SetSwimming(bool value)
    {
        IsSwimming = value;
        if (value)
            movement.AddSpeedLimit(swimSpeed);
        else
            movement.RemoveSpeedLimit(swimSpeed);
    }
}
