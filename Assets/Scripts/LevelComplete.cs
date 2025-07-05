using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelComplete : MonoBehaviour
{
    public GameObject LevelUI;
    public TextMeshProUGUI debug;
    public GameObject LevelEndUI;
    public GameManager gameManager;
    public GameObject indicator1, indicator2;
    public Button leftButton, rightButton;
    private string imageDir = "/Captures/";
    private int idx = 0;
    private bool done= false;
    [SerializeField] private InputActionReference left, right;

    void Update()
    {
        if (done == true)
        {
            indicator1.SetActive(false);
            indicator2.SetActive(false);
        }
    }
    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name != "Body") return;
        gameManager.SaveAllViolationImages();
        // debug.text = "triggered";
        LevelUI.SetActive(false);
        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Reached Destination Successfully    Final Score: {gameManager.score}";
        LevelEndUI.SetActive(true);
        done = true;
        Time.timeScale = 0.0f;
        leftButton.onClick.AddListener(PreviousImage);
        rightButton.onClick.AddListener(NextImage);
        SaveScore();
        DisplayImage(idx);
        GetComponent<MeshRenderer>().enabled = false;
    }

    void DisplayImage(int idx)
    {
        // string imagePath = imageDir + "violation_" + idx + ".png";
        string imagePath = Path.Combine(Application.persistentDataPath, "Captures", $"violation_{idx}.png");
        string message = gameManager.messageList[idx];

        // byte[] bytes = System.IO.File.ReadAllBytes(Application.dataPath + imagePath);
        // Texture2D texture = new Texture2D(3840, 2160);
        byte[] bytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2); // auto-resize with LoadImage
        if (texture.LoadImage(bytes))
        {
            LevelEndUI.transform.Find("Violation_Image").GetComponent<RawImage>().texture = texture;
            LevelEndUI.transform.Find("Violation_Text").GetComponent<TextMeshProUGUI>().text = $"Event {idx + 1}: {message}";
        }
        else
        {
            Debug.Log("Failed to load image");
        }
        left.action.Enable();
        right.action.Enable();
        // left.action.started += leftStart;
        // right.action.started += rightStart;
    }
    void SaveScore()
    {
        // string playerName = SessionManager.Instance.playerName;
        int finalScore = gameManager.score;
        // PlayerPrefs.SetInt($"Score_{playerName}", finalScore);
        // int id = int.Parse(playerName.Replace("Player", ""));
        // PlayerPrefs.SetInt("LastPlayerID", id);
        // // Add this player to the list of known score keys (for displaying later)
        // var keys = new HashSet<string>(PlayerPrefs.GetString("ScoreKeys", "").Split(','));
        // if (!keys.Contains(playerName))
        // {
        //     keys.Add(playerName);
        //     PlayerPrefs.SetString("ScoreKeys", string.Join(",", keys));
        // }
        PlayerPrefs.SetInt("LastScore", finalScore);
        PlayerPrefs.Save();
    }
    // void leftStart(InputAction.CallbackContext callbackContext)
    // {
    //     PreviousImage();
    // }
    // void rightStart(InputAction.CallbackContext callbackContext)
    // {
    //     NextImage();
    // }

    public void NextImage()
    {
        Debug.Log("Next");
        idx++;
        idx %= gameManager.fileCount;
        DisplayImage(idx);
    }

    public void PreviousImage(){
        Debug.Log("Previous");
        idx--;
        idx = (idx+gameManager.fileCount)%gameManager.fileCount;
        DisplayImage(idx);
    }
}
