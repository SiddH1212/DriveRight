using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManagerMobile : GameManagerBase
{
    public int initialScore = 100;
    public TextMeshProUGUI scoreText;
    // public GameObject mainCam;
    public InputActionReference backToMenu;
    private float relaxationTime = 5f;  
    public List<string> imagePaths = new List<string>();
    public List<float> timeStamps = new List<float>();
    public List<float> deltaScores = new List<float>();
    public float timeLimit = 100f;

    private List<Texture2D> violationImages = new List<Texture2D>();
    private HashSet <int> savedIndexes = new HashSet <int>();

    void Awake()
    {
        Time.timeScale = 1.0f;
    }

    public void Start()
    {
        // var roadGraph = FindObjectOfType<RoadGraph>();
        // roadGraph.RebuildGraph();
        score = initialScore;
        scoreText.text = "";
        backToMenu.action.Enable();
        backToMenu.action.started += back;
    }

    private void back(InputAction.CallbackContext callbackContext)
    {
        SceneManager.LoadScene("Start_Mobile");
    }

    private void OnDestroy()
    {
        backToMenu.action.started -= back;
    }

    public override void ReportLightCross(string lightColor)
    {
        int deltaScore;
        if (lightColor == "Red")
        {
            deltaScore = -20;
        }
        else if (lightColor == "Yellow")
        {
            deltaScore = 0;
        }
        else
        {
            deltaScore = 5;
        }

        UpdateScore(deltaScore, $"{lightColor} light crossed by the vehicle");
    }

    public override void UpdateScore(int deltaScore, string message = "")
    {
        score += deltaScore;
        deltaScores.Add(deltaScore);
        timeStamps.Add(Time.time);

        if (deltaScore > 0)
        {
            Debug.Log($"Score = {score} \t (+{deltaScore}) \n{message}");
            scoreText.color = Color.green;
            StartCoroutine(UpdateMessage(message, relaxationTime));
            messageList.Add($"{message} \t (+{deltaScore})");
        }
        else
        {
            Debug.Log($"Score = {score} \t ({deltaScore}) \n{message}");
            scoreText.color = Color.red;
            StartCoroutine(UpdateMessage(message, relaxationTime));
            messageList.Add($"{message} \t ({deltaScore})");
        }
    }

    void UpdateScoreText(string message)
    {
        scoreText.text = message;
        if (message != "") CaptureViolationImage();
    }

    public void CaptureViolationImage()
    {
        StartCoroutine(CaptureViolationImageCoroutine());
    }

    private IEnumerator UpdateMessage(string message, float time)
    {
        UpdateScoreText(message);
        yield return new WaitForSeconds(time);
        if (scoreText.text == message) UpdateScoreText("");
    }

    private IEnumerator CaptureViolationImageCoroutine()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;

        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        violationImages.Add(image);
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        string imagePath = Path.Combine(path, $"violation_{fileCount}.png");
        imagePaths.Add(imagePath);

        fileCount++;
    }

    public override void SaveAllViolationImages()
    {
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        for (int i = 0; i < violationImages.Count; i++)
        {
            byte[] bytes = violationImages[i].EncodeToPNG();
            string imagePath = Path.Combine(path, $"violation_{i}.png");
            File.WriteAllBytes(imagePath, bytes);
            imagePaths.Add(imagePath);
        }

        Debug.Log($"Saved {violationImages.Count} violation screenshots to {path}");
        violationImages.Clear();
    }

    public override void SaveImage(int idx)
    {
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        Debug.Log(idx);
        byte[] bytes = violationImages[idx].EncodeToPNG();
        string imagePath = Path.Combine(path, $"violation_{fileCount}.png");
        File.WriteAllBytes(imagePath, bytes);
        savedIndexes.Add(idx);
        Debug.Log($"Saved image for event {idx + 1}");
    }

    public void SaveRelevantViolationImages(int idx)
    {
        int prevIdx = (idx - 1 + fileCount) % fileCount;
        int nextIdx = (idx + 1) % fileCount;

        if (!savedIndexes.Contains(nextIdx))
            SaveImage(nextIdx);

        if (!savedIndexes.Contains(prevIdx))
            SaveImage(prevIdx);
    }

    // void OnDestroy()
    // {
    //     SaveAllViolationImages();       
    // }
    // void CaptureScreen(string message)
    // {
    //     WaitForEndOfFrame();
    //     Camera cam = mainCam.GetComponent<Camera>();
    //     Debug.Log(cam == null ? "Camera is null" : "Camera is assigned");

    //     int width = Screen.width;
    //     int height = Screen.height;

    //     RenderTexture rt = new RenderTexture(width, height, 24);
    //     cam.targetTexture = rt;
    //     RenderTexture.active = rt;

    //     cam.Render(); // Explicitly render the camera into the RenderTexture

    //     Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
    //     image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    //     image.Apply();

    //     cam.targetTexture = null;
    //     RenderTexture.active = null;
    //     Destroy(rt);

    //     byte[] bytes = image.EncodeToPNG();
    //     Destroy(image);

    //     string path = Application.dataPath + "/Captures/";
    //     if (!Directory.Exists(path))
    //         Directory.CreateDirectory(path);

    //     File.WriteAllBytes(path + fileCount + "_" + message + ".png", bytes);
    //     Debug.Log($"Created {path + fileCount}.png");

    //     fileCount++;
    // }
}