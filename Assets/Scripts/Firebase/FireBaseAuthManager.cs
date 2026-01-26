using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;
    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser User { get; private set; }

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

        Auth = FirebaseAuth.DefaultInstance;
    }

    public bool IsLoggedIn => User != null;

    public void SetUser(FirebaseUser user)
    {
        User = user;
        Debug.Log($"Logged in as UID: {user.UserId}");

        // 🔑 Correct place to write device data
        FirebaseDeviceWriter.Instance.WriteDevice();
    }

    public void SignInAnonymously()
    {
        Auth.SignInAnonymouslyAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Auth failed: {task.Exception}");
                    return;
                }

                SetUser(task.Result.User);
            });
    }
}
