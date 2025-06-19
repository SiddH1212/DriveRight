using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelComplete : MonoBehaviour
{
    public GameObject LevelUI;
    public GameObject LevelEndUI;
    public GameManager gameManager;
    private string imageDir = "/Captures/";
    private int idx = 0;
    void OnTriggerEnter(Collider collider){
        if (collider.gameObject.name != "Body") return;
        
        gameManager.SaveAllViolationImages();
        LevelUI.SetActive(false);
        LevelEndUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Reached Destination Successfully    Final Score: {gameManager.score}";
        LevelEndUI.SetActive(true);
        Time.timeScale = 0.0f;

        DisplayImage(idx);
    }

    void DisplayImage(int idx){
        string imagePath = imageDir + "violation_" + idx + ".png";
        string message = gameManager.messageList[idx];

        byte[] bytes = System.IO.File.ReadAllBytes(Application.dataPath + imagePath);
        Texture2D texture = new Texture2D(3840, 2160);
        if (texture.LoadImage(bytes)){
            LevelEndUI.transform.Find("Violation_Image").GetComponent<RawImage>().texture = texture;
            LevelEndUI.transform.Find("Violation_Text").GetComponent<TextMeshProUGUI>().text = $"Event {idx+1}: {message}";
        }
        else{
            Debug.Log("Failed to load image");
        }
    }

    public void NextImage(){
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
