using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EXIT : MonoBehaviour
{
    [SerializeField] private Button exit;

    void Start()
    {
        exit.onClick.AddListener(OnClickExit);
    }

    private void OnClickExit()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        switch (currentScene)
        {
            case "Mobile2":
            case "Night_Mobile":
                SceneManager.LoadScene("Start_Mobile");
                break;

            case "VR":
            case "Night_Quest":
                SceneManager.LoadScene("Start_Quest");
                break;

            default:
                Debug.LogWarning(
                    $"EXIT: Unknown scene '{currentScene}', defaulting to Start_Mobile"
                );
                SceneManager.LoadScene("Start_Mobile");
                break;
        }
    }
}
