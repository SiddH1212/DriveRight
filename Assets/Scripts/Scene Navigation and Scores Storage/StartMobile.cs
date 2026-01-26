using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections;

public class StartMobile : MonoBehaviour
{
    /* ---------------- UI REFERENCES ---------------- */

    public GameObject firstCanvas, nameCanvas, scoreCanvas;
    public TextMeshProUGUI previousScore;
    public Transform scoreListParent;
    public GameObject Loading;
    public Button Play, stats, quit, back1, back2, Basic, Advanced;
    public TMP_InputField nameInputField;
    public Toggle showTextToggle;

    [SerializeField] private GameObject firstCanvasFirst;
    [SerializeField] private GameObject nameCanvasFirst;
    [SerializeField] private GameObject scoreCanvasFirst;

    /* ---------------- SESSION ---------------- */

    private static bool prefsClearedThisSession = false;

    /* ===================== UNITY ===================== */

    void Awake()
    {
        DeviceManager.EnsureExists();

        if (!prefsClearedThisSession)
        {
            // Clear only gameplay-related prefs
            PlayerPrefs.DeleteKey("LastScore");
            PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.DeleteKey("AllPlayers");

            PlayerPrefs.Save();
            prefsClearedThisSession = true;
            Debug.Log("Gameplay PlayerPrefs cleared (DeviceId preserved)");
        }

        ClearCaptureDirectory();
    }

    void Start()
    {
        firstCanvas.SetActive(true);
        nameCanvas.SetActive(false);
        scoreCanvas.SetActive(false);

        if (InputMode.IsControllerConnected())
            EventSystem.current.SetSelectedGameObject(firstCanvasFirst);

        Play.onClick.AddListener(OnClickPlay);
        Basic.onClick.AddListener(() => OnClickStart("Basic"));
        Advanced.onClick.AddListener(() => OnClickStart("Advanced"));
        stats.onClick.AddListener(CheckStats);
        quit.onClick.AddListener(OnClickQuit);
        back1.onClick.AddListener(OnClickBack);
        back2.onClick.AddListener(OnClickBack);
        Loading.SetActive(false);
    }

    /* ===================== FLOW ===================== */

    private void OnClickPlay()
    {
        firstCanvas.SetActive(false);
        nameCanvas.SetActive(true);

        if (InputMode.IsControllerConnected())
            EventSystem.current.SetSelectedGameObject(nameCanvasFirst);
    }

    private void OnClickStart(string mode)
    {
        Loading.SetActive(true);

        // 🔑 START SUPABASE SESSION HERE
        SupabaseSessionService.Instance.StartSession(
            level: mode,
            language: LanguageTranslator.SelectedLanguage,
            isVR: AppMode.UseVR
        );

        if (mode == "Basic")
            StartCoroutine(LoadSceneAsync("Mobile2"));
        else if (mode == "Advanced")
            StartCoroutine(LoadSceneAsync("Night_Mobile"));
        else
            Debug.LogError($"Unknown mode: {mode}");
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
            yield return null;

        loadOperation.allowSceneActivation = true;
    }

    private void OnClickQuit()
    {
        Application.Quit();
    }

    /* ===================== STATS ===================== */

    private void CheckStats()
    {
        firstCanvas.SetActive(false);
        scoreCanvas.SetActive(true);
        LoadAndDisplayScores();

        if (InputMode.IsControllerConnected())
            EventSystem.current.SetSelectedGameObject(scoreCanvasFirst);
    }

    private void LoadAndDisplayScores()
    {
        string allPlayers = PlayerPrefs.GetString("AllPlayers", "");

        if (string.IsNullOrEmpty(allPlayers))
        {
            previousScore.text = "No previous scores";
            return;
        }

        var players = allPlayers.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        string displayText = "";

        foreach (var p in players)
        {
            int score = PlayerPrefs.GetInt($"Score_{p}", 0);
            displayText += $"{p}: {score}\n";
        }

        previousScore.text = displayText;
    }

    private void ClearCaptureDirectory()
    {
        string path = Path.Combine(Application.persistentDataPath, "Captures");

        if (!Directory.Exists(path))
            return;

        try
        {
            foreach (string file in Directory.GetFiles(path))
                File.Delete(file);

            Debug.Log("Cleared Captures directory (Mobile)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to clear Captures directory: {e.Message}");
        }
    }

    /* ===================== NAVIGATION ===================== */

    private void OnClickBack()
    {
        nameCanvas.SetActive(false);
        scoreCanvas.SetActive(false);
        firstCanvas.SetActive(true);

        if (InputMode.IsControllerConnected())
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstCanvasFirst);
        }
    }

    private void OnDestroy()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
