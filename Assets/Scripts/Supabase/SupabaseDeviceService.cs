using UnityEngine;
using System;

public class SupabaseDeviceService : MonoBehaviour
{
    public static SupabaseDeviceService Instance;

    [Serializable]
    private class DevicePayload
    {
        public string device_id;
        public string platform;
        public string first_seen;
        public string last_seen;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DeviceManager.EnsureExists();
        WriteDevice();
    }

    private void WriteDevice()
    {
        var payload = new DevicePayload
        {
            device_id = DeviceManager.DeviceId,
            platform = Application.platform.ToString(),
            first_seen = DateTime.UtcNow.ToString("o"),
            last_seen = DateTime.UtcNow.ToString("o")
        };

        string json = JsonUtility.ToJson(payload);

        StartCoroutine(
            SupabaseHttp.Post(
                "/rest/v1/devices",
                json,
                (success, response) =>
                {
                    if (success)
                        Debug.Log("[Supabase] Device written / updated");
                    else
                        Debug.LogError("[Supabase] Device write failed: " + response);
                }
            )
        );
    }
}
