using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    public string playerName;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
        }
        else
        {
            Destroy(gameObject); // Enforce singleton
        }
    }
}
