using UnityEngine;

public static class AppMode
{
    public static bool UseVR
    {
        get => PlayerPrefs.GetInt("UseVR", 0) == 1; // default = Non-VR
        set => PlayerPrefs.SetInt("UseVR", value ? 1 : 0);
    }
}
