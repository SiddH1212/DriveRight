using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseBootstrap : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    Debug.Log("Firebase initialized successfully");
                    FirebaseAuthManager.Instance.SignInAnonymously();
                }
                else
                {
                    Debug.LogError($"Firebase init failed: {task.Result}");
                }
            });
    }
}
