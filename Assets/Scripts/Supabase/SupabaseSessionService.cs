using UnityEngine;
using System;

public class SupabaseSessionService : MonoBehaviour
{
    public static SupabaseSessionService Instance;

    public string currentSessionId;
    public string CurrentSessionId => currentSessionId;

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
        }
    }

    /* ===================== START SESSION ===================== */

    [Serializable]
    private class StartSessionPayload
    {
        public string id;
        public string device_id;
        public string user_id;     // ✅ ADDED
        public string level;
        public string language;
        public bool is_vr;
        public string started_at;
    }

    public void StartSession(string level, string language, bool isVR)
    {
        if (string.IsNullOrEmpty(UserSession.CurrentUserId))
        {
            Debug.LogError("[Supabase] Cannot start session: no user selected");
            return;
        }

        var payload = new StartSessionPayload
        {
            id = Guid.NewGuid().ToString(),
            device_id = DeviceManager.DeviceId,
            user_id = UserSession.CurrentUserId, // ✅ THIS FIXES NULL
            level = level,
            language = language,
            is_vr = isVR,
            started_at = DateTime.UtcNow.ToString("o")
        };

        currentSessionId = payload.id;

        StartCoroutine(
            SupabaseHttp.Post(
                "/rest/v1/sessions",
                JsonUtility.ToJson(payload),
                (success, response) =>
                {
                    if (success)
                        Debug.Log($"[Supabase] Session started: {currentSessionId}");
                    else
                        Debug.LogError($"[Supabase] Session start failed: {response}");
                }
            )
        );
    }

    /* ===================== END SESSION ===================== */

    [Serializable]
    private class EndSessionPayload
    {
        public string ended_at;
        public int final_score;
    }

    public void EndSession(int finalScore)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("[Supabase] EndSession called with no active session");
            return;
        }

        var payload = new EndSessionPayload
        {
            ended_at = DateTime.UtcNow.ToString("o"),
            final_score = finalScore
        };

        StartCoroutine(
            SupabaseHttp.Patch(
                $"/rest/v1/sessions?id=eq.{currentSessionId}",
                JsonUtility.ToJson(payload),
                (success, response) =>
                {
                    if (success)
                        Debug.Log("[Supabase] Session ended successfully");
                    else
                        Debug.LogError($"[Supabase] Session end failed: {response}");
                }
            )
        );
    }
}
