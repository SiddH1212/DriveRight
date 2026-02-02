using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public static class SupabaseStorageService
{
    private const string BUCKET = "violations-screenshots";

    public static IEnumerator UploadScreenshot(
        // byte[] pngBytes,
        byte[] jpgBytes,
        string sessionId,
        int violationNumber,
        System.Action<bool, string> callback
    )
    {
        // string fileName = $"session_{sessionId}/event_{violationNumber}.png";
        string fileName = $"session_{sessionId}/event_{violationNumber}.jpg";

        string url =
            $"{SupabaseConfig.ProjectUrl}/storage/v1/object/{BUCKET}/{fileName}";

        var request = new UnityWebRequest(url, "POST");
        // request.uploadHandler = new UploadHandlerRaw(pngBytes);
        request.uploadHandler = new UploadHandlerRaw(jpgBytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        // request.SetRequestHeader("Content-Type", "image/png");
        request.SetRequestHeader("Content-Type", "image/jpeg");
        request.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
        request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.AnonKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string publicUrl =
                $"{SupabaseConfig.ProjectUrl}/storage/v1/object/public/{BUCKET}/{fileName}";

            callback(true, publicUrl);
        }
        else
        {
            callback(false, request.downloadHandler.text);
        }
    }
}
