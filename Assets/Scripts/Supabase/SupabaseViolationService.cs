using UnityEngine;
using System;

public class SupabaseViolationService : MonoBehaviour
{
    public static SupabaseViolationService Instance;

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

    public void LogViolation(
        int violationNumber,
        string message,
        int deltaScore,
        Texture2D screenshot
    )
    {
        if (SupabaseSessionService.Instance == null)
        {
            Debug.LogError("[Supabase] No active session");
            return;
        }

        string sessionId = SupabaseSessionService.Instance.CurrentSessionId;

        byte[] pngBytes = screenshot.EncodeToPNG();

        StartCoroutine(
            SupabaseStorageService.UploadScreenshot(
                pngBytes,
                sessionId,
                violationNumber,
                (success, screenshotUrl) =>
                {
                    if (!success)
                    {
                        Debug.LogError("[Supabase] Screenshot upload failed");
                        screenshotUrl = null;
                    }

                    var payload = new
                    {
                        id = Guid.NewGuid().ToString(), 
                        session_id = sessionId,
                        violation_number = violationNumber,
                        message = message,
                        delta_score = deltaScore,
                        screenshot_url = screenshotUrl
                    };

                    SupabaseHttp.Post(
                        endpoint: "/rest/v1/violations",
                        json: JsonUtility.ToJson(payload),
                        callback: (ok, response) =>
                        {
                            if (ok)
                                Debug.Log($"[Supabase] Violation {violationNumber} saved");
                            else
                            {
                                Debug.LogError("[Supabase] Violation insert failed");
                                Debug.LogError(response);
                            }
                        }
                    );
                }
            )
        );
    }
}
