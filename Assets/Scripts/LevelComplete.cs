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
    private string imageDir = "/Captures/";
    private int idx = 0;
    [SerializeField] private InputActionReference left, right;
    void OnTriggerEnter(Collider collider){
        if (collider.gameObject.name != "Body") return;
        gameManager.SaveAllViolationImages();
        // debug.text = "triggered";
        LevelUI.SetActive(false);
        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Reached Destination Successfully    Final Score: {gameManager.score}";
        LevelEndUI.SetActive(true);
        Time.timeScale = 0.0f;

        DisplayImage(idx);
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
        left.action.started += leftStart;
        right.action.started += rightStart;
    }
    void leftStart(InputAction.CallbackContext callbackContext)
    {
        PreviousImage();
    }
    void rightStart(InputAction.CallbackContext callbackContext)
    {
        NextImage();
    }

    public void NextImage()
    {
        idx++;
        idx %= gameManager.fileCount;
        DisplayImage(idx);
    }

    public void PreviousImage(){
        idx--;
        idx = (idx+gameManager.fileCount)%gameManager.fileCount;
        DisplayImage(idx);
    }
}
