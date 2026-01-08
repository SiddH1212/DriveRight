using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    void Start()
    {
        LevelUI.SetActive(true);
        LevelEndUI.SetActive(false);

        left.action.Enable();
        right.action.Enable();

        left.action.started += _ => PreviousImage();
        right.action.started += _ => NextImage();

        leftButton.onClick.AddListener(PreviousImage);
        rightButton.onClick.AddListener(NextImage);
    }

    void OnDestroy()
    {
        left.action.Disable();
        right.action.Disable();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (completed) return;
        if (!collider.CompareTag("Player")) return;

        completed = true;

        // Clamp score
        if (gameManager.score < 0)
            gameManager.score = 0;

        // Save scores (desktop parity)
        PlayerPrefs.SetInt("LastScore", gameManager.score);
        string playerName = PlayerPrefs.GetString("PlayerName", "Unknown");
        PlayerPrefs.SetInt($"Score_{playerName}", gameManager.score);
        PlayerPrefs.Save();

        // UI swap
        LevelUI.SetActive(false);
        LevelEndUI.SetActive(true);
        Time.timeScale = 0f;

        // Title
        LevelEndUI.transform.Find("Title")
            .GetComponent<TextMeshProUGUI>().text =
            "Reached Destination Successfully";

        // Score text
        LevelEndUI.transform.Find("Score")
            .GetComponent<TextMeshProUGUI>().text =
            $"Final Score: {gameManager.score}/100";

        // Rank text (desktop parity)
        var rankText = LevelEndUI.transform.Find("Rank")
            .GetComponent<TextMeshProUGUI>();

        if (gameManager.score > 75)
            rankText.text = "Safe Driving! Excellent Score";
        else if (gameManager.score > 50)
            rankText.text = "Defensive Driving! Good Score";
        else if (gameManager.score > 25)
            rankText.text = "Moderate Driving! Fair Score";
        else
            rankText.text = "Risky Driving! Poor Score";

        rankText.color = Color.yellow;

        // Save & show first violation (KEEP MOBILE LOGIC)
        gameManager.SaveImage(idx);
        DisplayImage(idx);

        // Disable trigger visuals
        if (TryGetComponent(out MeshRenderer mr))
            mr.enabled = false;
    }

    void DisplayImage(int index)
    {
        if (gameManager.fileCount == 0) return;

        index = Mathf.Clamp(index, 0, gameManager.fileCount - 1);

        string imagePath = Path.Combine(
            Application.persistentDataPath,
            "Captures",
            $"violation_{index}.png"
        );

        if (!File.Exists(imagePath)) return;

        byte[] bytes = File.ReadAllBytes(imagePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        LevelEndUI.transform.Find("Violation_Image")
            .GetComponent<RawImage>().texture = tex;

        string message = gameManager.messageList[index];
        var text = LevelEndUI.transform.Find("Violation_Text")
            .GetComponent<TextMeshProUGUI>();

        text.text = $"Event {index + 1}: {message}";
        text.color = gameManager.deltaScores[index] < 0 ? Color.red : Color.green;

        // KEEP MOBILE CONTEXT SAVE
        gameManager.SaveRelevantViolationImages(index);
    }

    public void NextImage()
    {
        if (!completed) return;

        idx = (idx + 1) % gameManager.fileCount;
        DisplayImage(idx);
    }

    public void PreviousImage()
    {
        if (!completed) return;

        idx = (idx - 1 + gameManager.fileCount) % gameManager.fileCount;
        DisplayImage(idx);
    }
}
