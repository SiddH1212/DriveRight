using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        // Match Desktop behavior: clear prefs once per app session
        if (!prefsClearedThisSession)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            prefsClearedThisSession = true;
            Debug.Log("PlayerPrefs cleared at game start (Mobile)");
        }
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

    public void OnConfirmName()
    {
        string playerName = nameInputField.text.Trim();

        // Desktop-compatible fallback
        if (string.IsNullOrEmpty(playerName))
        {
            int lastID = PlayerPrefs.GetInt("LastPlayerID", 0) + 1;
            PlayerPrefs.SetInt("LastPlayerID", lastID);
            playerName = $"Player{lastID}";
        }

        PlayerPrefs.SetString("PlayerName", playerName);

        // Track all players (Desktop-compatible)
        string allPlayers = PlayerPrefs.GetString("AllPlayers", "");
        var players = new HashSet<string>(
            allPlayers.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
        );
        players.Add(playerName);

        PlayerPrefs.SetString("AllPlayers", string.Join(",", players));

        if (!PlayerPrefs.HasKey($"Score_{playerName}"))
            PlayerPrefs.SetInt($"Score_{playerName}", 0);

        PlayerPrefs.Save();

        // Match Desktop behavior
        GameManager.SelectedshowText = showTextToggle.isOn;

        Debug.Log($"Player name set: {playerName}");
    }

    private void OnClickStart(string mode)
    {
        Loading.SetActive(true);

        if (mode == "Basic")
            StartCoroutine(LoadSceneAsync("Mobile2"));
        else if (mode == "Advanced")
            StartCoroutine(LoadSceneAsync("Night_Mobile"));
        else
            Debug.LogError($"Unknown mode: {mode}");
    }
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 1. Start loading
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        // 2. Prevent automatic scene switch
        loadOperation.allowSceneActivation = false;

        // 3. While still loading...
        while (loadOperation.progress < 0.9f)
        {
            // Progress is happening here (0 → 0.9)
            yield return null; // ← THIS keeps the game alive
        }

        // 4. Scene is ready → switch
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
