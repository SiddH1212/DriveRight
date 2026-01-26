using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public class GameManager : GameManagerBase
{
    public TextMeshProUGUI scoreText;
    public GameObject mainCam;
    [HideInInspector] public List<LaneNode> currentPath = new List<LaneNode>();
    // private Pathfinder pathfinder;
    [SerializeField] private Transform destination;
    // [SerializeField] private PathRenderer pathRenderer;

    private List<Texture2D> violationImages = new List<Texture2D>();

    void Awake()
    {
        Time.timeScale = 1.0f;
    }

    public void Start()
    {
        // var roadGraph = FindObjectOfType<RoadGraph>();
        // roadGraph.RebuildGraph();
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

        UpdateScore(deltaScore, $"{lightColor} crossed by the vehicle");
    }

    public override void UpdateScore(int deltaScore, string message = "")
    {
        score += deltaScore;

        if (deltaScore > 0)
        {
            Debug.Log($"Score = {score} \t (+{deltaScore}) \n{message}");
            UpdateScoreText($"Score = {score} \t (+{deltaScore}) \n{message}");
            messageList.Add($"{message} \t (+{deltaScore})");
        }
        else
        {
            Debug.Log($"Score = {score} \t ({deltaScore}) \n{message}");
            UpdateScoreText($"Score = {score} \t ({deltaScore}) \n{message}");
            messageList.Add($"{message} \t ({deltaScore})");
        }
    }

    void UpdateScoreText(string message)
    {
        scoreText.text = message;
        CaptureViolationImage();
    }

    public void CaptureViolationImage()
    {
        StartCoroutine(CaptureViolationImageCoroutine());
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
            File.WriteAllBytes(Path.Combine(path, $"violation_{i}.png"), bytes);
        }

        Debug.Log($"Saved {violationImages.Count} violation screenshots to {path}");
        violationImages.Clear();
    }

    public override void SaveImage(int idx)
    {
        // intentionally empty
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