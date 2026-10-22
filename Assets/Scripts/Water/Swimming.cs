using System;
using UnityEngine;
using Zenject;

// Water deeper than swimDepth slows the player to GameConfig.SwimSpeed. Goes
// through FirstPersonMovement's speed overrides, as Crouch does, so walking and
// running on land are untouched. Staying afloat is Buoyancy's job.
[RequireComponent(typeof(Buoyancy), typeof(FirstPersonMovement))]
public class Swimming : MonoBehaviour
{
    [Inject] private GameConfig config;

    [Tooltip("Depth of the feet under the surface from which the player swims instead of wading")]
    [SerializeField] private float swimDepth = 0.9f;

    private Buoyancy buoyancy;
    private FirstPersonMovement movement;
    private Func<float> swimSpeed;

    public bool IsSwimming { get; private set; }

    private void Awake()
    {
        buoyancy = GetComponent<Buoyancy>();
        movement = GetComponent<FirstPersonMovement>();
        swimSpeed = () => config.SwimSpeed;
    }

    private void FixedUpdate()
    {
        bool isDeep = buoyancy.Depth > swimDepth;
        if (isDeep != IsSwimming)
            SetSwimming(isDeep);
    }

    private void OnDisable()
    {
        if (IsSwimming)
            SetSwimming(false);
    }

    private void SetSwimming(bool value)
    {
        IsSwimming = value;
        if (value)
            movement.speedOverrides.Add(swimSpeed);
        else
            movement.speedOverrides.Remove(swimSpeed);
    }
}
