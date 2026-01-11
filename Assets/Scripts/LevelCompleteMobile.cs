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
        {
            Destroy(runtimeTexture);
            runtimeTexture = null;
        }
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

        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text =
            LanguageTranslator.Instance.Translate("Reached Destination Successfully");

        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().color = Color.green;

        LevelEndUI.transform.Find("Score").GetComponent<TextMeshProUGUI>().text =
            $"{LanguageTranslator.Instance.Translate("Final Score")}: {gameManager.score}/100";

        LevelEndUI.transform.Find("Rank").GetComponent<TextMeshProUGUI>().text =
            GetRankText(gameManager.score);

        Time.timeScale = 0f;
        if (TryGetComponent(out MeshRenderer mr))
            mr.enabled = false;
        if (gameManager.ViolationCount > 0)
        {
            // idx = gameManager.ViolationCount - 1;
            idx=0;
            DisplayImage(idx);
        }
    }

    public void ShowLevelComplete(int finalScore)
    {
        if (completed) return;
        completed = true;

        gameManager.score = Mathf.Max(0, finalScore);
        SetupNavigation();
        SaveScore();

        LevelUI.SetActive(false);
        LevelEndUI.SetActive(true);
        ApplyFonts();

        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text =
            LanguageTranslator.Instance.Translate("Failed to Reach Destination");

        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().color = Color.red;

        LevelEndUI.transform.Find("Score").GetComponent<TextMeshProUGUI>().text =
            $"{LanguageTranslator.Instance.Translate("Final Score")}: {gameManager.score}/100";

        LevelEndUI.transform.Find("Rank").GetComponent<TextMeshProUGUI>().text = "";

        Time.timeScale = 0f;

        if (gameManager.ViolationCount > 0)
        {
            // idx = gameManager.ViolationCount - 1;
            idx=0;
            DisplayImage(idx);
        }
    }

   void DisplayImage(int index)
    {
        if (index < 0 || index >= gameManager.ViolationCount) return;

        var record = gameManager.GetViolation(index);
        if (string.IsNullOrEmpty(record.imagePath) || !File.Exists(record.imagePath))
            return;

        // Destroy ONLY the previous runtime texture
        if (runtimeTexture != null)
        {
            Destroy(runtimeTexture);
            runtimeTexture = null;
        }

        byte[] bytes = File.ReadAllBytes(record.imagePath);

        runtimeTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        runtimeTexture.LoadImage(bytes);

        var raw = LevelEndUI.transform
            .Find("Violation_Image")
            .GetComponent<RawImage>();

        raw.texture = runtimeTexture;

        var text = LevelEndUI.transform
            .Find("Violation_Text")
            .GetComponent<TextMeshProUGUI>();

        text.text = $"Event {index + 1}: {record.message}";
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

    void SaveScore()
    {
        PlayerPrefs.SetInt("LastScore", gameManager.score);
        string name = PlayerPrefs.GetString("PlayerName", "Unknown");
        PlayerPrefs.SetInt($"Score_{name}", gameManager.score);
        PlayerPrefs.Save();
    }

    string GetRankText(int score)
    {
        if (score > 75) return LanguageTranslator.Instance.Translate("Safe Driving! Excellent Score");
        if (score > 50) return LanguageTranslator.Instance.Translate("Defensive Driving! Good Score");
        if (score > 25) return LanguageTranslator.Instance.Translate("Moderate Driving! Fair Score");
        return LanguageTranslator.Instance.Translate("Risky Driving! Poor Score");
    }
}
