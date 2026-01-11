using UnityEngine;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManagerMobile : GameManagerBase
{
    /* ===================== SCORE ===================== */

    public int initialScore = 100;
    public TextMeshProUGUI scoreText;

    /* ===================== NOTIFICATIONS ===================== */

    public TextMeshProUGUI notifText;
    public float notifDisplayDuration = 1.5f;
    private Coroutine notifCoroutine;

    /* ===================== TIME ===================== */

    public float gracePeriod = 30f;
    private bool timerExpired;

    /* ===================== INPUT ===================== */

    public InputActionReference backToMenu;

    /* ===================== INTERNAL ===================== */

    private HashSet<int> savedIndexes = new();

    /* ===================== AUDIO ===================== */

    [SerializeField] private AudioSource violationAudioSource;
    [SerializeField] private AudioClip violationClip;
    [SerializeField] private float violationSoundCooldown = 0.5f;
    private float lastViolationSoundTime = -10f;

    /* ===================== UNITY ===================== */

    private Pathfinder pathfinder;
    [SerializeField] private Transform destination;
    [SerializeField] private PathRenderer pathRenderer;
    public float updateInterval = 0.3f;
    public float deviationThreshold = 20f;
    public float cutThreshold = 5f;
    public DisplayInstructions instructionDisplay;
    public LevelCompleteMobile levelComplete;
    private Vector3 lastPathStartPos;

    [SerializeField] private Transform player;
    [SerializeField] private TextMeshProUGUI CountDown;

    void Awake()
    {
        Time.timeScale = 1f;
    }

    public IEnumerator Start()
    {
        gameplayActive = false;
        score = initialScore;
        elapsedTime = 0f;
        timerExpired = false;
        graceActive = false;

        scoreText.text = "";
        notifText.text = "";
        notifText.alpha = 0f;

        backToMenu.action.Enable();
        backToMenu.action.started += BackToMenu;

        yield return StartCoroutine(InitializePathfinding());
        yield return StartCoroutine(StartupCountdown(5));

        gameplayActive = true;
        StartCoroutine(UpdatePathLoop());
    }

    void Update()
    {
        if (!gameplayActive) return;

        elapsedTime += Time.deltaTime;

        if (!timerExpired && elapsedTime >= timeLimit)
        {
            timerExpired = true;
            StartCoroutine(HandleTimeExpired());
        }
    }

    IEnumerator StartupCountdown(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            CountDown.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        CountDown.text = "Go!";
        yield return new WaitForSecondsRealtime(1f);
        CountDown.text = "";
    }

    /* ===================== TIME ===================== */

    private IEnumerator HandleTimeExpired()
    {
        graceActive = true;
        ShowNotification("Time exceeded! Grace period started.");

        float graceEnd = elapsedTime + gracePeriod;
        while (elapsedTime < graceEnd)
            yield return null;

        if (graceActive && levelComplete != null)
            levelComplete.ShowLevelComplete(0);
    }

    /* ===================== SCORING ===================== */

    public override void ReportLightCross(string lightColor)
    {
        int delta = lightColor switch
        {
            "Red" => -20,
            "Yellow" => 0,
            _ => 5
        };

        ShowNotification($"{lightColor} light crossed");
        UpdateScore(delta, $"{lightColor} light crossed by the vehicle");
    }

    public override void UpdateScore(int deltaScore, string message = "")
    {
        score += deltaScore;
        scoreText.color = deltaScore >= 0 ? Color.green : Color.red;

        if (deltaScore < 0)
            PlayViolationSound();

        // Restore legacy tracking
        // messageList.Add($"{message} ({deltaScore:+#;-#;0})");
        // deltaScores.Add(deltaScore);
        // timeStamps.Add(Time.time);

        StartCoroutine(UpdateScoreMessage(message, 5f));
        StartCoroutine(CaptureAndSaveViolation(deltaScore, message));
    }

    private IEnumerator UpdateScoreMessage(string message, float duration)
    {
        if (!string.IsNullOrEmpty(message))
            scoreText.text = message;

        yield return new WaitForSeconds(duration);

        if (scoreText.text == message)
            scoreText.text = "";
    }

    private IEnumerator CaptureAndSaveViolation(int deltaScore, string message)
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(
            Screen.width,
            Screen.height,
            TextureFormat.RGB24,
            false
        );

        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        var record = new ViolationRecord
        {
            imagePath = null,
            message = $"{message} ({deltaScore:+#;-#;0})",
            deltaScore = deltaScore,
            time = elapsedTime
        };

        violations.Add(record);
        SaveViolationToDisk(violations.Count - 1, tex);
    }

    private void SaveViolationToDisk(int idx, Texture2D tex)
    {
        if (savedIndexes.Contains(idx)) return;

        string dir = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"violation_{idx}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        Destroy(tex); // SAFE: runtime texture

        violations[idx].imagePath = path;
        savedIndexes.Add(idx);
    }

    /* ===================== NOTIFICATIONS ===================== */

    public void ShowNotification(string message)
    {
        notifText.text = message;
        notifText.alpha = 1f;

        if (notifCoroutine != null)
            StopCoroutine(notifCoroutine);

        notifCoroutine = StartCoroutine(HideNotificationAfterDelay());
    }

    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notifDisplayDuration);
        notifText.text = "";
        notifText.alpha = 0f;
        notifCoroutine = null;
    }

    /* ===================== PATH ===================== */

    IEnumerator UpdatePathLoop()
    {
        while (!gameplayActive)
            yield return null;

        while (true)
        {
            if (currentPath.Count > 1 &&
                Vector3.Distance(player.position, currentPath[1].Position) < cutThreshold)
                currentPath.RemoveAt(0);

            if (HasDeviatedFromPath(player.position))
            {
                var newPath = pathfinder.GetPath(player.position, destination.position);
                if (newPath != null && newPath.Count > 1)
                    currentPath = newPath;
            }

            Vector3 start = player.position - player.forward * 2f;

            if (Vector3.Distance(start, lastPathStartPos) > 1f)
            {
                pathRenderer.DrawWorldPath(currentPath, start);
                lastPathStartPos = start;
            }

            instructionDisplay.UpdateInstruction();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    IEnumerator InitializePathfinding()
    {
        while (pathfinder == null ||
               pathfinder.roadGraph == null ||
               !pathfinder.roadGraph.IsReady)
        {
            yield return null;
            pathfinder = FindObjectOfType<Pathfinder>();
        }

        currentPath = pathfinder.GetPath(player.position, destination.position)
                      ?? new List<LaneNode>();

        pathRenderer.DrawWorldPath(
            currentPath,
            player.position - player.forward * 2f
        );
    }

    bool HasDeviatedFromPath(Vector3 pos)
    {
        if (currentPath == null || currentPath.Count == 0)
            return true;

        int checkCount = Mathf.Min(2, currentPath.Count);
        float minDist = float.MaxValue;

        for (int i = 0; i < checkCount; i++)
            minDist = Mathf.Min(minDist,
                Vector3.Distance(pos, currentPath[i].Position));

        return minDist > deviationThreshold;
    }

    private void PlayViolationSound()
    {
        if (Time.time - lastViolationSoundTime < violationSoundCooldown)
            return;

        violationAudioSource?.PlayOneShot(violationClip);
        lastViolationSoundTime = Time.time;
    }

    private void BackToMenu(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene("Start_Mobile");
    }

    public override void SaveAllViolationImages() { }
}
