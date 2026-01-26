using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerWatcher : MonoBehaviour
{
    public static bool IsControllerConnected { get; private set; }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        RefreshState();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad || device is Joystick)
            RefreshState();
    }

    void RefreshState()
    {
        IsControllerConnected =
            Gamepad.all.Count > 0 || Joystick.all.Count > 0;
    }
}
