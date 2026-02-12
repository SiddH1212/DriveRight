using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class StartMobile : MonoBehaviour
{
    /* ===================== CANVASES ===================== */

    public GameObject firstCanvas;
    public GameObject nameCanvas;
    public GameObject userCanvas;
    public GameObject instructionsCanvas;
    public GameObject Loading;

    /* ===================== FIRST CANVAS ===================== */

    public Button playButton;
    public Button instructionsButton;
    public Button quitButton;

    /* ===================== NAME CANVAS ===================== */

    public TMP_Dropdown userDropdown;
    public TMP_Dropdown languageDropdown;
    public Button basicButton;
    public Button advancedButton;
    public Button backFromNameButton;
    public Button createUserButton;

    /* ===================== USER CANVAS ===================== */

    public TMP_InputField userNameInput;
    public TMP_InputField userEmailInput;
    public Button submitUserButton;
    public Button backFromUserButton;

    /* ===================== STATE ===================== */

    private List<SupabaseUserService.User> loadedUsers = new();

    /* ===================== UNITY ===================== */

    void Awake()
    {
        DeviceManager.EnsureExists();
        ClearCaptureDirectory();
    }

    void Start()
    {
        ShowFirstCanvas();

        playButton.onClick.AddListener(OpenNameCanvas);
        instructionsButton.onClick.AddListener(OpenInstructions);
        quitButton.onClick.AddListener(Application.Quit);

        backFromNameButton.onClick.AddListener(ShowFirstCanvas);
        backFromUserButton.onClick.AddListener(OpenNameCanvas);

        createUserButton.onClick.AddListener(OpenUserCanvas);
        submitUserButton.onClick.AddListener(CreateUser);

        basicButton.onClick.AddListener(() => StartGame("Basic"));
        advancedButton.onClick.AddListener(() => StartGame("Advanced"));

        Loading.SetActive(false);
    }

    /* ===================== CANVAS FLOW ===================== */

    void ShowFirstCanvas()
    {
        firstCanvas.SetActive(true);
        nameCanvas.SetActive(false);
        userCanvas.SetActive(false);
        instructionsCanvas.SetActive(false);
    }

    void OpenNameCanvas()
    {
        firstCanvas.SetActive(false);
        nameCanvas.SetActive(true);
        userCanvas.SetActive(false);
        instructionsCanvas.SetActive(false);

        StartCoroutine(LoadUsers());
    }

    void OpenUserCanvas()
    {
        nameCanvas.SetActive(false);
        userCanvas.SetActive(true);
    }

    void OpenInstructions()
    {
        firstCanvas.SetActive(false);
        instructionsCanvas.SetActive(true);
    }

    /* ===================== USERS ===================== */

    IEnumerator LoadUsers()
    {
        userDropdown.ClearOptions();
        loadedUsers.Clear();
        // Placeholder option
        var options = new List<string> { "Select User" };
        yield return SupabaseUserService.Instance.FetchUsers(users =>
        {
            loadedUsers = users;

            // var options = new List<string>();
            foreach (var u in users)
                options.Add(u.display_name);

            userDropdown.AddOptions(options);
            // Force placeholder selection
            userDropdown.value = 0;
            userDropdown.RefreshShownValue();
        });
    }

    void CreateUser()
    {
        string name = userNameInput.text.Trim();
        string email = userEmailInput.text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
        {
            Debug.LogWarning("Name and Email are required");
            return;
        }

        SupabaseUserService.Instance.CreateUser(name, email, (success, user) =>
        {
            if (!success)
            {
                Debug.LogError("User creation failed");
                return;
            }

            UserSession.CurrentUserId = user.id;
            UserSession.CurrentUserName = user.display_name;
            UserSession.CurrentUserEmail = user.email;

            OpenNameCanvas();
        });
    }

    /* ===================== START GAME ===================== */

    void StartGame(string mode)
    {

        // if (userDropdown.options.Count == 0)
        // {
        //     Debug.LogWarning("No user selected");
        //     return;
        // }
        if (userDropdown.value == 0)
        {
            Debug.LogWarning("User must be selected");
            return;
        }
        // var selectedUser = loadedUsers[userDropdown.value];
        var selectedUser = loadedUsers[userDropdown.value - 1];

        // PlayerPrefs.SetString("SELECTED_USER_ID", selectedUser.id);
        // PlayerPrefs.SetString("SELECTED_USER_NAME", selectedUser.display_name);
        // PlayerPrefs.Save();
        UserSession.CurrentUserId = selectedUser.id;
        UserSession.CurrentUserName = selectedUser.display_name;
        UserSession.CurrentUserEmail = selectedUser.email;
        Loading.SetActive(true);

        SupabaseSessionService.Instance.StartSession(
            level: mode,
            language: LanguageTranslator.SelectedLanguage,
            isVR: AppMode.UseVR
        );

        if (mode == "Basic")
            StartCoroutine(LoadSceneAsync("Mobile2"));
        else
            StartCoroutine(LoadSceneAsync("Night_Mobile"));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }

    /* ===================== CLEANUP ===================== */

    void ClearCaptureDirectory()
    {
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.GetFiles(path))
            File.Delete(file);
    }
}
