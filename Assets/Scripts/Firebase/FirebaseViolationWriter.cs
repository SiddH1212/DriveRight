using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class FirebaseViolationWriter : MonoBehaviour
{
    public static FirebaseViolationWriter Instance;

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
        Debug.Log("[FirebaseViolationWriter] Ready");
    }

    public void WriteViolation(
        string message,
        int deltaScore,
        float time
    )
    {
        if (FirebaseSessionManager.Instance == null ||
            string.IsNullOrEmpty(FirebaseSessionManager.Instance.CurrentSessionId))
        {
            Debug.LogWarning("[FirebaseViolationWriter] No active session");
            return;
        }

        string sessionId = FirebaseSessionManager.Instance.CurrentSessionId;
        string violationId = Guid.NewGuid().ToString();

        var data = new Dictionary<string, object>
        {
            { "message", message },
            { "delta_score", deltaScore },
            { "time", time },
            { "recorded_at", DateTime.UtcNow.ToString("o") }
        };

        root.Child("sessions")
            .Child(sessionId)
            .Child("violations")
            .Child(violationId)
            .SetValueAsync(data);

        Debug.Log($"[FirebaseViolationWriter] Violation logged ({deltaScore})");
    }
}
