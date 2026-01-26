using UnityEngine;
using System;

public static class DeviceManager
{
    private const string DeviceIdKey = "DEVICE_ID";
    private static string cachedDeviceId;

    public static string DeviceId
    {
        get
        {
            if (string.IsNullOrEmpty(cachedDeviceId))
                cachedDeviceId = LoadOrCreate();

            return cachedDeviceId;
        }
    }

    public static void EnsureExists()
    {
        _ = DeviceId;
    }

    private static string LoadOrCreate()
    {
        if (PlayerPrefs.HasKey(DeviceIdKey))
            return PlayerPrefs.GetString(DeviceIdKey);

        string id = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(DeviceIdKey, id);
        PlayerPrefs.Save();

        Debug.Log($"[DeviceManager] Created DeviceId: {id}");
        return id;
    }
}
