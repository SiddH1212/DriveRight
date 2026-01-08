using UnityEngine;
using TMPro;
using System; 
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

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
    private bool graceActive;

    /* ===================== INPUT ===================== */

    public InputActionReference backToMenu;

    /* ===================== INTERNAL ===================== */

    private float relaxationTime = 5f;
    private List<Texture2D> violationImages = new();
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
    public float updateInterval = 0.3f;     // Interval (sec) after which the rendered path is updated
    public float deviationThreshold = 20f;  // After how much deviation should we recalculate the path
    public float cutThreshold = 5f;         // After being how close to the next node show we discard the current node on the path
    public DisplayInstructions instructionDisplay;
    public LevelCompleteMobile levelComplete;
    private Vector3 lastPathStartPos;

    [SerializeField] private Transform player;
    [SerializeField]private TextMeshProUGUI CountDown;

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
        if (!gameplayActive)
        return;
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

    private void OnDestroy()
    {
        backToMenu.action.started -= BackToMenu;
    }

    /* ===================== TIME ===================== */

    private IEnumerator HandleTimeExpired()
    {
        graceActive = true;
        ShowNotification("Time exceeded! Grace period started.");

        float graceEnd = elapsedTime + gracePeriod;
        while (elapsedTime < graceEnd)
            yield return null;

        if (graceActive)
            EndLevel();
    }

    private void EndLevel()
    {
        SaveRelevantViolationImages(fileCount - 1);
        SceneManager.LoadScene("Start_Mobile");
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
        Debug.Log($"UpdateScore called at {Time.time}, delta = {deltaScore}");
        score += deltaScore;

        scoreText.color = deltaScore >= 0 ? Color.green : Color.red;
        StartCoroutine(UpdateScoreMessage(message, relaxationTime));

        messageList.Add($"{message} ({deltaScore:+#;-#;0})");
        // Play violation sound ONLY for negative scores
        if (deltaScore < 0)
        {
            PlayViolationSound();
        }
    }
    private void PlayViolationSound()
    {
        if (violationAudioSource == null || violationClip == null)
            return;

        // Prevent rapid spam
        if (Time.time - lastViolationSoundTime < violationSoundCooldown)
            return;
        Debug.Log($"Playing violation sound at {Time.time}");
        violationAudioSource.PlayOneShot(violationClip);
        lastViolationSoundTime = Time.time;
    }
    private IEnumerator UpdateScoreMessage(string message, float duration)
    {
        scoreText.text = message;
        CaptureViolationImage();

        yield return new WaitForSeconds(duration);

        if (scoreText.text == message)
            scoreText.text = "";
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

    /* ===================== CAPTURE ===================== */

    public void CaptureViolationImage()
    {
        StartCoroutine(CaptureViolationImageCoroutine());
    }

    private IEnumerator CaptureViolationImageCoroutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        image.Apply();

        violationImages.Add(image);
        fileCount++;
    }

    /* ===================== SAVING ===================== */

    // Disabled on mobile (performance)
    public override void SaveAllViolationImages() { }

    //  Save single image (safe)
    public override void SaveImage(int idx)
    {
        if (idx < 0 || idx >= violationImages.Count) return;
        if (savedIndexes.Contains(idx)) return;

        string path = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        File.WriteAllBytes(
            Path.Combine(path, $"violation_{idx}.png"),
            violationImages[idx].EncodeToPNG()
        );

        savedIndexes.Add(idx);
    }

    public void SaveRelevantViolationImages(int idx)
    {
        if (fileCount == 0) return;

        SaveImage(idx);
        SaveImage((idx - 1 + fileCount) % fileCount);
        SaveImage((idx + 1) % fileCount);
    }


    IEnumerator UpdatePathLoop()
    {
        while(!gameplayActive)
            yield return null;
        while (true)
        {
            if (currentPath.Count > 1 &&
                Vector3.Distance(player.transform.position, currentPath[1].Position) < cutThreshold)
            {
                currentPath.RemoveAt(0);
            }

            if (HasDeviatedFromPath(player.transform.position))
            {
                var newPath = pathfinder.GetPath(player.transform.position, destination.position);
                if (newPath != null && newPath.Count > 1)
                    currentPath = newPath;
            }

            Vector3 pathStart = player.position - player.forward * 2f;

            // Only redraw if player moved enough OR path changed
            if (Vector3.Distance(pathStart, lastPathStartPos) > 1.0f)
            {
                pathRenderer.DrawWorldPath(currentPath, pathStart);
                lastPathStartPos = pathStart;
            }
            instructionDisplay.UpdateInstruction();
            yield return new WaitForSeconds(updateInterval);
        }
    }
    IEnumerator InitializePathfinding()
    {
        // -------- PATHFINDING PARITY --------
        // pathfinder = FindObjectOfType<Pathfinder>();
        while (pathfinder == null ||
            pathfinder.roadGraph == null ||
            !pathfinder.roadGraph.IsReady)
        {
            yield return null;
            pathfinder = FindObjectOfType<Pathfinder>();
        }

        var path = pathfinder.GetPath(player.position, destination.position);
        currentPath = path ?? new List<LaneNode>();

        pathRenderer.DrawWorldPath(
            currentPath,
            player.transform.position - player.transform.forward * 2f
        );
    }
    bool HasDeviatedFromPath(Vector3 pos)
    {
        if (currentPath == null || currentPath.Count == 0)
            return true;

        int checkCount = Mathf.Min(2, currentPath.Count);
        float minDist = float.MaxValue;

        for (int i = 0; i < checkCount; i++)
        {
            float dist = Vector3.Distance(pos, currentPath[i].Position);
            if (dist < minDist)
                minDist = dist;
        }

        return minDist > deviationThreshold;
    }
    /* ===================== INPUT ===================== */

    private void BackToMenu(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene("Start_Mobile");
    }
}
