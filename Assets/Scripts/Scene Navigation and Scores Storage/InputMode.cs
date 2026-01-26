using UnityEngine;
using UnityEngine.InputSystem;

public static class InputMode
{
    public static bool IsControllerConnected()
    {
        // New Input System (preferred)
        return Gamepad.current != null
               || Joystick.current != null;
        // return Gamepad.all.Count > 0 || Joystick.all.Count > 0;
    }
}
