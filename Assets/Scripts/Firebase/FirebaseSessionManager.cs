using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class FirebaseSessionManager : MonoBehaviour
{
    public static FirebaseSessionManager Instance;

    private const string DATABASE_URL =
        "https://driveright-3203f-default-rtdb.asia-southeast1.firebasedatabase.app";

    private DatabaseReference root;

    public string CurrentSessionId { get; private set; }

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
        Debug.Log("[FirebaseSessionManager] Ready");
    }

    public void StartSession(string level, string language, bool isVR)
    {
        if (FirebaseAuthManager.Instance?.User == null)
        {
            Debug.LogError("[FirebaseSessionManager] Auth not ready");
            return;
        }

        CurrentSessionId = Guid.NewGuid().ToString();

        var data = new Dictionary<string, object>
        {
            { "device_id", DeviceManager.DeviceId },
            { "firebase_uid", FirebaseAuthManager.Instance.User.UserId },
            { "level", level },
            { "language", language },
            { "is_vr", isVR },
            { "started_at", DateTime.UtcNow.ToString("o") }
        };

        root.Child("sessions")
            .Child(CurrentSessionId)
            .SetValueAsync(data);

        Debug.Log($"[FirebaseSessionManager] Session started: {CurrentSessionId}");
    }

    public void EndSession(int finalScore)
    {
        if (string.IsNullOrEmpty(CurrentSessionId))
        {
            Debug.LogWarning("[FirebaseSessionManager] No active session");
            return;
        }

        var updates = new Dictionary<string, object>
        {
            { "ended_at", DateTime.UtcNow.ToString("o") },
            { "final_score", finalScore }
        };

        root.Child("sessions")
            .Child(CurrentSessionId)
            .UpdateChildrenAsync(updates);

        Debug.Log($"[FirebaseSessionManager] Session ended: {CurrentSessionId}");
    }
}
