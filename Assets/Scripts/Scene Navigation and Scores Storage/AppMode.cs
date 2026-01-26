// using UnityEngine;

// public static class AppMode
// {
//     public static bool UseVR
//     {
//         get => PlayerPrefs.GetInt("UseVR", 0) == 1; // default = Non-VR
//         set => PlayerPrefs.SetInt("UseVR", value ? 1 : 0);
//     }
// }
using UnityEngine;

public static class AppMode
{
    private static bool useVR = false;

    public static bool UseVR
    {
        get => useVR;
        set => useVR = value;
    }
}
