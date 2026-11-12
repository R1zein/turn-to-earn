using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Zenject;
using Cursor = UnityEngine.Cursor;

public enum InputMode
{
    Gameplay,   // on foot: cursor locked, Player map
    Ui,         // a panel holds the mouse: cursor free, only Ui map
    Drone,      // cursor locked: the mouse moves the drone's aim, Drone map
}

// The only reader of Controls. Turns presses into game events and exposes held
// state; owns the cursor through a stack of modes, so closing a panel opened over
// another panel returns to the one below. The scene's base mode cannot be popped.
public class InputService : IInitializable, ITickable, IDisposable
{
    [Inject] private Controls controls;
    [Inject] private GameConfig config;
    [Inject] private InputMode baseMode;

    private readonly Stack<InputMode> modes = new Stack<InputMode>();

    public event Action OnAttack;
    public event Action OnInteract;
    public event Action OnJump;
    public event Action OnBuildMenu;
    public event Action OnBuildConfirm;
    public event Action OnBuildRaise;
    public event Action OnBuildLower;
    // A number key: 0 for 1, 1 for 2 and so on.
    public event Action<int> OnToolSlot;
    public event Action OnUiClose;

    public InputMode Mode => modes.Peek();

    // On foot. Move is the keys as they are, x — strafe, y — forward; easing in and
    // out of a walk is PlayerMovement's acceleration, not the input's.
    public Vector2 Move => controls.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => controls.Player.Look.ReadValue<Vector2>() * config.LookScale;
    public bool IsRunHeld => controls.Player.Run.IsPressed();
    public bool IsCrouchHeld => controls.Player.Crouch.IsPressed();
    public float BuildRotate => controls.Player.BuildRotate.ReadValue<float>() * config.ScrollScale;

    // Drone. Look is the raw mouse delta in pixels; the axes are -1..1: throttle + = faster,
    // yaw + = right, roll + = right, lift + = up.
    // Fire is read as edges so DroneShooting keeps its press/release logic.
    public Vector2 DroneLook => controls.Drone.Look.ReadValue<Vector2>();
    public float DroneThrottle => controls.Drone.Throttle.ReadValue<float>();
    public float DroneYaw => controls.Drone.Yaw.ReadValue<float>();
    public float DroneRoll => controls.Drone.Roll.ReadValue<float>();
    public float DroneLift => controls.Drone.Lift.ReadValue<float>();
    public bool IsBoostHeld => controls.Drone.Boost.IsPressed();
    public bool IsDroneAimHeld => controls.Drone.Aim.IsPressed();
    public bool DroneBombDropped => controls.Drone.Bomb.WasPressedThisFrame();
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
            return;

        var player = controls.Player;
        if (player.Attack.WasPressedThisFrame()) OnAttack?.Invoke();
        if (player.Interact.WasPressedThisFrame()) OnInteract?.Invoke();
        if (player.Jump.WasPressedThisFrame()) OnJump?.Invoke();
        if (player.BuildConfirm.WasPressedThisFrame()) OnBuildConfirm?.Invoke();
        if (player.BuildRaise.WasPressedThisFrame()) OnBuildRaise?.Invoke();
        if (player.BuildLower.WasPressedThisFrame()) OnBuildLower?.Invoke();
        if (player.ToolSlot.WasPressedThisFrame()) OnToolSlot?.Invoke(Slot(player.ToolSlot.activeControl));
        if (player.BuildMenu.WasPressedThisFrame()) OnBuildMenu?.Invoke();
    }

    // The digit keys are consecutive in Key, from Digit1 to Digit9.
    private static int Slot(InputControl key) => ((KeyControl)key).keyCode - Key.Digit1;

    private void ApplyMode()
    {
        InputMode mode = Mode;
        // On foot and in the drone the mouse steers by its movement: locked and hidden.
        Cursor.lockState = mode == InputMode.Ui ? CursorLockMode.None : CursorLockMode.Locked;
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
}
