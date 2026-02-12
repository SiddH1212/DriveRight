using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public class SupabaseUserService : MonoBehaviour
{
    public static SupabaseUserService Instance;

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

    /* ===================== MODELS ===================== */

    [Serializable]
    public class User
    {
        public string id;
        public string display_name;
        public string email;
    }

    [Serializable]
    private class UserListWrapper
    {
        public List<User> items;
    }

    /* ===================== CREATE USER ===================== */

    public void CreateUser(string name, string email, Action<bool, User> callback)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
        {
            Debug.LogError("[SupabaseUserService] Name or email missing");
            callback(false, null);
            return;
        }

        var payload = new User
        {
            id = Guid.NewGuid().ToString(),
            display_name = name,
            email = email
        };

        StartCoroutine(
            SupabaseHttp.Post(
                "/rest/v1/users",
                JsonUtility.ToJson(payload),
                (success, response) =>
                {
                    if (!success)
                    {
                        Debug.LogError("[SupabaseUserService] User creation failed");
                        Debug.LogError(response);
                        callback(false, null);
                        return;
                    }

                    callback(true, payload);
                }
            )
        );
    }

    /* ===================== FETCH USERS ===================== */

    public IEnumerator FetchUsers(Action<List<User>> callback)
    {
        var request = new UnityWebRequest(
            SupabaseConfig.ProjectUrl + "/rest/v1/users?select=id,display_name,email&order=created_at.asc",
            "GET"
        );

        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
        request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.AnonKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[SupabaseUserService] Failed to fetch users");
            Debug.LogError(request.downloadHandler.text);
            callback(new List<User>());
            yield break;
        }

        // Supabase returns a raw JSON array → wrap it for JsonUtility
        string rawJson = request.downloadHandler.text;

        if (string.IsNullOrEmpty(rawJson) || rawJson == "[]")
        {
            callback(new List<User>());
            yield break;
        }

        string wrappedJson = "{\"items\":" + rawJson + "}";
        var wrapper = JsonUtility.FromJson<UserListWrapper>(wrappedJson);

        callback(wrapper != null && wrapper.items != null
            ? wrapper.items
            : new List<User>());
    }
}
