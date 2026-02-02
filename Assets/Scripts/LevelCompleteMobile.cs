using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO;

public class LevelCompleteMobile : MonoBehaviour
{
    public GameObject LevelUI;
    public GameObject LevelEndUI;
    public GameManagerMobile gameManager;

    [Header("Navigation")]
    public Button leftButton;
    public Button rightButton;
    [SerializeField] private InputActionReference left;
    [SerializeField] private InputActionReference right;

    private int idx = 0;
    private bool completed = false;
    private Texture2D runtimeTexture;

    void Start()
    {
        LevelUI.SetActive(true);
        LevelEndUI.SetActive(false);
    }

    void OnDestroy()
    {
        left.action.Disable();
        right.action.Disable();

        if (runtimeTexture != null)
            Destroy(runtimeTexture);
    }

    void OnTriggerEnter(Collider col)
    {
        if (completed || !col.CompareTag("Player")) return;
        completed = true;

        SetupNavigation();
        SaveScore();

        LevelUI.SetActive(false);
        LevelEndUI.SetActive(true);
        ApplyFonts();

        var title = LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        title.font = LanguageTranslator.Instance.GetFont();
        title.text = LanguageTranslator.Instance.Translate("Reached Destination Successfully");
        title.color = Color.green;

        var scoreTMP = LevelEndUI.transform.Find("Score").GetComponent<TextMeshProUGUI>();
        scoreTMP.font = LanguageTranslator.Instance.GetFont();
        scoreTMP.text =
            $"{LanguageTranslator.Instance.Translate("Safety Score")}: {gameManager.score}/100";

        var rankTMP = LevelEndUI.transform.Find("Rank").GetComponent<TextMeshProUGUI>();
        rankTMP.font = LanguageTranslator.Instance.GetFont();
        rankTMP.text = GetRankText(gameManager.score);

        Time.timeScale = 0f;

        if (gameManager.ViolationCount > 0)
        {
            idx = 0;
            gameManager.SaveRelevantViolationImages(idx);
            DisplayImage(idx);
        }
    }

    public void ShowLevelComplete(int finalScore)
    {
        if (completed) return;
        completed = true;

        gameManager.score = Mathf.Max(0, finalScore);
        gameManager.score = Mathf.Min(100, gameManager.score);
        SetupNavigation();
        SaveScore();

        LevelUI.SetActive(false);
        LevelEndUI.SetActive(true);
        ApplyFonts();

        var title = LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        title.font = LanguageTranslator.Instance.GetFont();
        title.text = LanguageTranslator.Instance.Translate("Failed to Reach Destination");
        title.color = Color.red;

        var scoreTMP = LevelEndUI.transform.Find("Score").GetComponent<TextMeshProUGUI>();
        scoreTMP.font = LanguageTranslator.Instance.GetFont();
        scoreTMP.text =
            $"{LanguageTranslator.Instance.Translate("Safety Score")}: {gameManager.score}/100";

        var rankTMP = LevelEndUI.transform.Find("Rank").GetComponent<TextMeshProUGUI>();
        rankTMP.font = LanguageTranslator.Instance.GetFont();
        rankTMP.text = GetRankText(gameManager.score);

        Time.timeScale = 0f;

        if (gameManager.ViolationCount > 0)
        {
            idx = 0;
            gameManager.SaveRelevantViolationImages(idx);
            DisplayImage(idx);
        }
    }

    void SaveScore()
    {
        PlayerPrefs.SetInt("LastScore", gameManager.score);
        PlayerPrefs.Save();

        // ✅ SAFE BACKEND CALL
        if (SupabaseSessionService.Instance != null)
        {
            SupabaseSessionService.Instance.EndSession(gameManager.score);
        }
        else
        {
            Debug.LogWarning(
                "[Supabase] Session service not available when ending level"
            );
        }
    }

    void DisplayImage(int index)
    {
        if (index < 0 || index >= gameManager.ViolationCount) return;

        gameManager.SaveRelevantViolationImages(index);

        string path = Path.Combine(
            Application.persistentDataPath,
            "Captures",
            // $"event_{index}.png"
            $"event_{index}.jpg"
        );

        if (!File.Exists(path)) return;

        if (runtimeTexture != null)
            Destroy(runtimeTexture);

        runtimeTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        runtimeTexture.LoadImage(File.ReadAllBytes(path));

        LevelEndUI.transform.Find("Violation_Image")
            .GetComponent<RawImage>().texture = runtimeTexture;

        var record = gameManager.GetViolation(index);
        var text = LevelEndUI.transform.Find("Violation_Text")
            .GetComponent<TextMeshProUGUI>();

        text.font = LanguageTranslator.Instance.GetFont();
        text.text =
            $"Event {index + 1}: {LanguageTranslator.Instance.Translate(record.message)} " +
            $"({record.deltaScore:+#;-#;0})";

        text.color = record.deltaScore < 0 ? Color.red : Color.green;
    }

    public void NextImage()
    {
        if (gameManager.ViolationCount == 0) return;
        idx = (idx + 1) % gameManager.ViolationCount;
        DisplayImage(idx);
    }

    public void PreviousImage()
    {
        if (gameManager.ViolationCount == 0) return;
        idx = (idx - 1 + gameManager.ViolationCount) % gameManager.ViolationCount;
        DisplayImage(idx);
    }

    void SetupNavigation()
    {
        left.action.Enable();
        right.action.Enable();

        left.action.started += _ => PreviousImage();
        right.action.started += _ => NextImage();

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();

        leftButton.onClick.AddListener(PreviousImage);
        rightButton.onClick.AddListener(NextImage);
    }

    void ApplyFonts()
    {
        var font = LanguageTranslator.Instance.GetFont();

        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().font = font;
        LevelEndUI.transform.Find("Score").GetComponent<TextMeshProUGUI>().font = font;
        LevelEndUI.transform.Find("Rank").GetComponent<TextMeshProUGUI>().font = font;
        LevelEndUI.transform.Find("Violation_Text").GetComponent<TextMeshProUGUI>().font = font;
    }

    string GetRankText(int score)
    {
        if (score > 75) return LanguageTranslator.Instance.Translate("Safe Driving! Excellent Score");
        if (score > 50) return LanguageTranslator.Instance.Translate("Defensive Driving! Good Score");
        if (score > 25) return LanguageTranslator.Instance.Translate("Moderate Driving! Fair Score");
        return LanguageTranslator.Instance.Translate("Risky Driving! Poor Score");
    }
}
