using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;
using Cursor = UnityEngine.Cursor;

public enum InputMode
{
    Gameplay,   // on foot: cursor locked, Player map
    Ui,         // a panel holds the mouse: cursor free, only Ui map
    Drone,      // cursor confined and hidden (the drone draws its own reticle), Drone map
}

// The only reader of Controls. Turns presses into game events and exposes held
// state; owns the cursor through a stack of modes, so closing a panel opened over
// another panel returns to the one below. The scene's base mode cannot be popped.
public class InputService : IInitializable, ITickable, IDisposable
{
    // Smoothing of the old Input Manager "Horizontal"/"Vertical" axes
    // (gravity 3, sensitivity 3, snap). Walking was tuned against it.
    private const float AxisSensitivity = 3f;
    private const float AxisGravity = 3f;

    [Inject] private Controls controls;
    [Inject] private GameConfig config;
    [Inject] private InputMode baseMode;

    private readonly Stack<InputMode> modes = new Stack<InputMode>();
    private Vector2 move;

    public event Action OnAttack;
    public event Action OnInteract;
    public event Action OnJump;
    public event Action OnBuildMenu;
    public event Action OnBuildConfirm;
    public event Action OnToolNext;
    public event Action OnToolPrevious;
    public event Action OnUiClose;

    public InputMode Mode => modes.Peek();

    // On foot.
    public Vector2 Move => move;
    public Vector2 Look => controls.Player.Look.ReadValue<Vector2>() * config.LookScale;
    public bool IsRunHeld => controls.Player.Run.IsPressed();
    public bool IsCrouchHeld => controls.Player.Crouch.IsPressed();
    public float BuildRotate => controls.Player.BuildRotate.ReadValue<float>() * config.ScrollScale;

    // Drone. Fire is read as edges so DroneShooting keeps its press/release logic.
    public Vector2 DroneAim => controls.Drone.Aim.ReadValue<Vector2>();
    public bool IsThrottleHeld => controls.Drone.Throttle.IsPressed();
    public bool FireStarted => controls.Drone.Fire.WasPressedThisFrame();
    public bool FireEnded => controls.Drone.Fire.WasReleasedThisFrame();

    public void Initialize()
    {
        modes.Push(baseMode);
        ApplyMode();
    }

    public void Dispose()
    {
        controls.Player.Disable();
        controls.Drone.Disable();
        controls.Ui.Disable();
    }

    public void PushMode(InputMode mode)
    {
        modes.Push(mode);
        ApplyMode();
    }

    public void PopMode()
    {
        if (modes.Count > 1)
            modes.Pop();
        ApplyMode();
    }

    public void Tick()
    {
        // Ui first: a press that opens a panel this frame must not also close it.
        if (controls.Ui.enabled && controls.Ui.Close.WasPressedThisFrame())
            OnUiClose?.Invoke();

        if (!controls.Player.enabled)
        {
            move = Vector2.zero;
            return;
        }

        Vector2 rawMove = controls.Player.Move.ReadValue<Vector2>();
        move.x = SmoothAxis(move.x, rawMove.x);
        move.y = SmoothAxis(move.y, rawMove.y);

        var player = controls.Player;
        if (player.Attack.WasPressedThisFrame()) OnAttack?.Invoke();
        if (player.Interact.WasPressedThisFrame()) OnInteract?.Invoke();
        if (player.Jump.WasPressedThisFrame()) OnJump?.Invoke();
        if (player.BuildConfirm.WasPressedThisFrame()) OnBuildConfirm?.Invoke();
        if (player.ToolNext.WasPressedThisFrame()) OnToolNext?.Invoke();
        if (player.ToolPrevious.WasPressedThisFrame()) OnToolPrevious?.Invoke();
        if (player.BuildMenu.WasPressedThisFrame()) OnBuildMenu?.Invoke();
    }

    private void ApplyMode()
    {
        InputMode mode = Mode;
        Cursor.lockState = mode == InputMode.Gameplay ? CursorLockMode.Locked
            : mode == InputMode.Drone ? CursorLockMode.Confined
            : CursorLockMode.None;
        Cursor.visible = mode == InputMode.Ui;

        SetEnabled(controls.Player, mode == InputMode.Gameplay);
        SetEnabled(controls.Drone, mode == InputMode.Drone);
        SetEnabled(controls.Ui, mode == InputMode.Ui);
    }

    private static void SetEnabled(InputActionMap map, bool isEnabled)
    {
        if (isEnabled)
            map.Enable();
        else
            map.Disable();
    }

    // Same rule as an Input Manager key axis: move toward the pressed direction at
    // `sensitivity`, snap through zero when reversing, fall back at `gravity`.
    private static float SmoothAxis(float value, float target)
    {
        float dt = Time.deltaTime;
        if (target == 0f)
            return Mathf.MoveTowards(value, 0f, AxisGravity * dt);

        if (value != 0f && Mathf.Sign(value) != Mathf.Sign(target))
            value = 0f;
        return Mathf.MoveTowards(value, target, AxisSensitivity * dt);
    }
}
