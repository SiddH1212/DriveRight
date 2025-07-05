using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public int initialScore = 100;
    public int score = 100;
    public TextMeshProUGUI scoreText;
    public GameObject mainCam;
    public InputActionReference backToMenu;
    private float relaxationTime = 5f;  
    public int fileCount = 0;
    public List<string> messageList = new List<string>();
    public List<string> imagePaths = new List<string>();
    public List<float> timeStamps = new List<float>();
    public List<float> deltaScores = new List<float>();
    public float timeLimit = 100f;
    private List<Texture2D> violationImages = new List<Texture2D>();
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
    public void ReportLightCross(string lightColor)
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
    public void UpdateScore(int deltaScore, string message = "")
    {
        score += deltaScore;
        deltaScores.Add(deltaScore);
        timeStamps.Add(Time.time);
        if (deltaScore > 0)
        {
            Debug.Log($"Score = {score} \t (+{deltaScore}) \n{message}");
            // UpdateScoreText($"Score = {score} \t (+{deltaScore}) \n{message}");
            // UpdateScoreText($"{message}");
            scoreText.color = Color.green;
            StartCoroutine(UpdateMessage(message, relaxationTime));
            messageList.Add($"{message} \t (+{deltaScore})");
        }
        else
        {
            Debug.Log($"Score = {score} \t ({deltaScore}) \n{message}");
            // UpdateScoreText($"Score = {score} \t ({deltaScore}) \n{message}");
            // UpdateScoreText($"{message}");
            scoreText.color = Color.red;
            StartCoroutine(UpdateMessage(message, relaxationTime));
            messageList.Add($"{message} \t ({deltaScore})");
        }
    }
    void UpdateScoreText(string message)
    {
        scoreText.text = message;
        // CaptureViolationImage();
        if (message != "") CaptureViolationImage();
    }

    public void CaptureViolationImage()
    {
        StartCoroutine(CaptureViolationImageCoroutine());
    }

    // Coroutine for the message to stop being displayed after 'time' seconds
    private IEnumerator UpdateMessage(string message, float time)
    {
        UpdateScoreText(message);
        yield return new WaitForSeconds(time);
        if (scoreText.text == message) UpdateScoreText("");
    }
    private IEnumerator CaptureViolationImageCoroutine()
    {
        yield return new WaitForEndOfFrame(); // wait until UI + scene is fully rendered
        Camera cam = mainCam.GetComponent<Camera>();
        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();
        // Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        // image.Apply();
        image.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        cam.targetTexture = null;
        RenderTexture.active = null;

        // violationImages.Add(image);
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        // string imagePath = path + "violation_" + fileCount + ".png";
        string imagePath = Path.Combine(path, $"violation_{fileCount}.png");
        
        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(imagePath, bytes);
        imagePaths.Add(imagePath);
        // Debug.Log("Captured violation screenshot (stored in memory)");
        fileCount++;
        Destroy(rt);
        Destroy(image);
    }

    public void SaveAllViolationImages()
    {
        // string path = Application.dataPath + "/Captures/";
        string path = Path.Combine(Application.persistentDataPath, "Captures");
        // if (!Directory.Exists(path))
        //     Directory.CreateDirectory(path);

        // for (int i = 0; i < violationImages.Count; i++)
        // {
        //     byte[] bytes = violationImages[i].EncodeToPNG();
        //     // File.WriteAllBytes(path + "violation_" + i + ".png", bytes);
        //     File.WriteAllBytes(Path.Combine(path, $"violation_{i}.png"), bytes);
        // }

        Debug.Log($"Saved {violationImages.Count} violation screenshots to {path}");
        violationImages.Clear(); // clear memory after saving
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