using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class FirebaseDeviceWriter : MonoBehaviour
{
    public static FirebaseDeviceWriter Instance;

    private const string DATABASE_URL =
        "https://driveright-3203f-default-rtdb.asia-southeast1.firebasedatabase.app";

    private DatabaseReference root;

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

        root = FirebaseDatabase.GetInstance(DATABASE_URL).RootReference;
        Debug.Log("[FirebaseDeviceWriter] Database reference initialized");
    }

    public void WriteDevice()
    {
        if (FirebaseAuthManager.Instance?.User == null)
        {
            Debug.LogError("[FirebaseDeviceWriter] Auth not ready");
            return;
        }

        string deviceId = DeviceManager.DeviceId;
        var deviceRef = root.Child("devices").Child(deviceId);

        deviceRef.GetValueAsync().ContinueWith(task =>
        {
            bool exists = task.Result.Exists;

            var data = new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                {
                    "first_seen",
                    exists
                        ? task.Result.Child("first_seen").Value.ToString()
                        : DateTime.UtcNow.ToString("o")
                },
                { "last_seen", DateTime.UtcNow.ToString("o") }
            };

            deviceRef.SetValueAsync(data);

            Debug.Log($"[FirebaseDeviceWriter] Device {(exists ? "updated" : "created")}");
        });
    }
}
