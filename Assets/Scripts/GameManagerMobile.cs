using UnityEngine;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerMobile : GameManagerBase
{
    /* ===================== SCORE ===================== */

    public int initialScore = 100;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI scoreText2;

    /* ===================== VIOLATIONS ===================== */

    [SerializeField] private int maxViolations = 10;
    private int violationCount = 0;
    private bool violationsEnabled = false; // ✅ NEW

    /* ===================== VIOLATION BAR ===================== */

    [Header("Violation Bar")]
    [SerializeField] private Image violationBarFill;
    [SerializeField] private Image violationBarFill2;
    [SerializeField] private float barAnimSpeed = 6f;
    private Coroutine barAnim;

    /* ===================== NOTIFICATIONS ===================== */

    public TextMeshProUGUI notifText;
    public float notifDisplayDuration = 1.5f;
    private Coroutine notifCoroutine;

    /* ===================== INPUT ===================== */

    public InputActionReference backToMenu;

    /* ===================== INTERNAL ===================== */

    private HashSet<int> savedIndexes = new();

    /* ===================== AUDIO ===================== */

    [SerializeField] private AudioSource violationAudioSource;
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
        violationCount = 0;
        violationsEnabled = false; // ✅ ensure disabled at start

        scoreText.text = "";
        scoreText2.text = "";
        notifText.text = "";
        notifText.alpha = 0f;

        UpdateViolationBarInstant();

        backToMenu.action.Enable();
        backToMenu.action.started += BackToMenu;

        yield return StartCoroutine(InitializePathfinding());
        yield return StartCoroutine(StartupCountdown(5));

        gameplayActive = true;
        StartCoroutine(UpdatePathLoop());
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

        violationsEnabled = true; // ✅ violations start AFTER countdown
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
        scoreText2.color = deltaScore >= 0 ? Color.green : Color.red;

        if (deltaScore < 0 && violationsEnabled) // ✅ guarded
        {
            violationCount++;
            PlayViolationSound();

            ShowViolationMessage(message);
            UpdateViolationBar();

            if (violationCount >= maxViolations)
            {
                StartCoroutine(HandleFinalViolation(deltaScore, message));
                return;
            }

            StartCoroutine(CaptureViolation(deltaScore, message));
        }
        else if (deltaScore >= 0 && violationsEnabled) // ✅ guarded
        {
            StartCoroutine(UpdateScoreMessage(message, 5f));
            StartCoroutine(CaptureViolation(deltaScore, message));
        }
    }

    /* ===================== FINAL VIOLATION FIX ===================== */

    private IEnumerator HandleFinalViolation(int deltaScore, string message)
    {
        yield return StartCoroutine(CaptureViolation(deltaScore, message));
        yield return null; // allow frame to finish
        EndGameDueToViolations();
    }

    private void ShowViolationMessage(string message)
    {
        if (notifCoroutine != null)
            StopCoroutine(notifCoroutine);

        if(AppMode.UseVR)
            scoreText2.text = $"Violation {violationCount}/{maxViolations}\n{message}";
        else
            scoreText.text = $"Violation {violationCount}/{maxViolations}\n{message}";

        notifCoroutine = StartCoroutine(ClearScoreTextAfterDelay(4f));
    }

    private IEnumerator ClearScoreTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        scoreText.text = "";
        notifCoroutine = null;
    }

    private IEnumerator UpdateScoreMessage(string message, float duration)
    {
        if (!string.IsNullOrEmpty(message))
            scoreText.text = message;

        yield return new WaitForSeconds(duration);

        if (scoreText.text == message)
            scoreText.text = "";
    }

    /* ===================== VIOLATION BAR ===================== */

    private void UpdateViolationBar()
    {
        // if (violationBarFill == null)
        //     return;

        if (barAnim != null)
            StopCoroutine(barAnim);

        barAnim = StartCoroutine(AnimateViolationBar());
    }

    private void UpdateViolationBarInstant()
    {
        // if (violationBarFill == null)
        //     return;

        float t = (float)violationCount / maxViolations;
        if(!AppMode.UseVR)
            // violationBarFill.fillAmount = 1f - Mathf.Clamp01(t);
            violationBarFill.fillAmount = Mathf.Clamp01(t);
        else
            // violationBarFill2.fillAmount = 1f - Mathf.Clamp01(t);
            violationBarFill2.fillAmount = Mathf.Clamp01(t);
    }

    private IEnumerator AnimateViolationBar()
    {
        float start;
        if(!AppMode.UseVR)
            start = violationBarFill.fillAmount;
        else
            start = violationBarFill2.fillAmount;
        // float target = 1f - (float)violationCount / maxViolations;
        float target = (float)violationCount / maxViolations;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * barAnimSpeed;
            if(!AppMode.UseVR)
                violationBarFill.fillAmount = Mathf.Lerp(start, target, t);
            else
                violationBarFill2.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }
        if(!AppMode.UseVR)
            violationBarFill.fillAmount = target;
        else
            violationBarFill2.fillAmount = target;
    }

    /* ===================== END GAME ===================== */

    private void EndGameDueToViolations()
    {
        gameplayActive = false;
        ShowNotification("Too many violations!");

        if (levelComplete != null)
            levelComplete.ShowLevelComplete(0);
    }

    private IEnumerator CaptureViolation(int deltaScore, string message)
    {
        // if (deltaScore >= 0)
        //     yield break;
        if(!violationsEnabled)
            yield break;

        yield return new WaitForEndOfFrame();

        Texture2D full = new Texture2D(
            Screen.width,
            Screen.height,
            TextureFormat.RGB24,
            false
        );

        full.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        full.Apply();

        // 🔥 Resize to 1280x720
        Texture2D image = ResizeTexture(full, 1280, 720);
        Destroy(full);

        // ✅ violationNumber derived here (NO extra state)
        int violationNumber = violations.Count + 1;

        violations.Add(new ViolationRecord
        {
            image = image,
            imagePath = null,
            // message = $"{message} ({deltaScore:+#;-#;0})",
            message = message,
            deltaScore = deltaScore,
            time = elapsedTime
        });

        // ✅ SAFE Supabase hook (optional, never crashes gameplay)
        if (SupabaseViolationService.Instance != null)
        {
            SupabaseViolationService.Instance.LogViolation(
                violationNumber,
                message,
                deltaScore,
                image
            );
        }
    }

    /* ===================== SAVING ===================== */

    public override void SaveImage(int idx)
    {
        if (idx < 0 || idx >= violations.Count) return;
        if (savedIndexes.Contains(idx)) return;
        if (violations[idx].image == null) return;

        string dir = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // string path = Path.Combine(dir, $"event_{idx}.png");
        // File.WriteAllBytes(path, violations[idx].image.EncodeToPNG());
        string path = Path.Combine(dir, $"event_{idx}.jpg");
        File.WriteAllBytes(path, violations[idx].image.EncodeToJPG(60));

        Destroy(violations[idx].image);
        violations[idx].image = null;
        violations[idx].imagePath = path;

        savedIndexes.Add(idx);
    }

    public void SaveRelevantViolationImages(int idx)
    {
        if (violations.Count == 0) return;

        SaveImage(idx);
        SaveImage((idx - 1 + violations.Count) % violations.Count);
        SaveImage((idx + 1) % violations.Count);
    }
    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
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

        float minDist = float.MaxValue;
        int checkCount = Mathf.Min(2, currentPath.Count);

        for (int i = 0; i < checkCount; i++)
            minDist = Mathf.Min(minDist,
                Vector3.Distance(pos, currentPath[i].Position));

        return minDist > deviationThreshold;
    }

    private void PlayViolationSound()
    {
        if (Time.time - lastViolationSoundTime < violationSoundCooldown)
            return;

        violationAudioSource?.Play();
        lastViolationSoundTime = Time.time;
    }

    private void BackToMenu(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene("Start_Mobile");
    }

    public override void SaveAllViolationImages() { }
}
