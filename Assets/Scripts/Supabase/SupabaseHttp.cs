using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public static class SupabaseHttp
{
    /* ===================== POST (INSERT / UPSERT) ===================== */

    public static IEnumerator Post(
        string endpoint,
        string json,
        System.Action<bool, string> callback
    )
    {
        var request = new UnityWebRequest(
            SupabaseConfig.ProjectUrl + endpoint,
            "POST"
        );

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
        request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.AnonKey);

        // UPSERT support
        request.SetRequestHeader("Prefer", "resolution=merge-duplicates");
        // ✅ CORRECT FOR INSERTS
        // request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        callback?.Invoke(success, request.downloadHandler.text);
    }

    /* ===================== PATCH (UPDATE EXISTING ROW) ===================== */

    public static IEnumerator Patch(
        string endpoint,
        string json,
        System.Action<bool, string> callback
    )
    {
        var request = new UnityWebRequest(
            SupabaseConfig.ProjectUrl + endpoint,
            "PATCH"
        );

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
        request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.AnonKey);

        // Required for updates
        request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        callback?.Invoke(success, request.downloadHandler.text);
    }
}
