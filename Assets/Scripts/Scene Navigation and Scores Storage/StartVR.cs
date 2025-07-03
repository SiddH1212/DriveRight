using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartVR : MonoBehaviour
{
    public GameObject firstCanvas, nameCanvas, scoreCanvas, scoreLinePrefab;
    public TextMeshProUGUI player, previousScore;
    public Transform scoreListParent;
    public Button Play, start, stats, quit, back1, back2;
    void Start()
    {
        firstCanvas.SetActive(true);
        nameCanvas.SetActive(false);
        scoreCanvas.SetActive(false);
        Play.onClick.AddListener(onClickPlay);
        start.onClick.AddListener(onClickStart);
        stats.onClick.AddListener(checkStats);
        quit.onClick.AddListener(onClickQuit);
        back1.onClick.AddListener(onClickBack);
        back2.onClick.AddListener(onClickBack);
    }

    public void onClickPlay()
    {
        // string playerName = GetNextAvailablePlayerName();

        // // Store it in SessionManager (if used)
        // SessionManager.Instance.playerName = playerName;

        // // Show the player name on screen
        // firstCanvas.SetActive(false);
        // nameCanvas.SetActive(true);
        // player.text = $"You are {playerName}";
        SceneManager.LoadScene("VR");
    }
    // string GetNextAvailablePlayerName()
    // {
    //     int lastID = PlayerPrefs.GetInt("LastPlayerID", 0);

    //     // Look for the next unused or unsaved name
    //     while (true)
    //     {
    //         lastID++;
    //         string key = $"Player{lastID}";
    //         if (!PlayerPrefs.HasKey($"Score_{key}"))
    //         {
    //             return key;
    //         }
    //     }
    // }
    public void onClickStart()
    {
        SceneManager.LoadScene("VR");
    }
    public void onClickQuit()
    {
        Application.Quit();
    }
    void LoadAndDisplayScores()
    {
        // Clear existing entries
        foreach (Transform child in scoreListParent)
        {
            Destroy(child.gameObject);
        }
        // string allKeys = PlayerPrefs.GetString("ScoreKeys", "");
        // Debug.Log($"Loaded ScoreKeys: {allKeys}");
        // var keyList = new HashSet<string>(allKeys.Split(','));
        // foreach (var key in keyList)
        // {
        //     if (string.IsNullOrWhiteSpace(key)) continue;

        //     int score = PlayerPrefs.GetInt($"Score_{key}", -1);
        //     Debug.Log($"Found score for {key}: {score}");

        //     GameObject entry = Instantiate(scoreLinePrefab, scoreListParent);
        //     entry.GetComponent<TextMeshProUGUI>().text = $"{key}: {score}";
        // }
        int score = PlayerPrefs.GetInt("LastScore");
        previousScore.text = $"Previous Score: {score}";
    }
    public void checkStats()
    {
        firstCanvas.SetActive(false);
        scoreCanvas.SetActive(true);
        LoadAndDisplayScores();
    }

    public void onClickBack()
    {
        nameCanvas.SetActive(false);
        scoreCanvas.SetActive(false);
        firstCanvas.SetActive(true);
    }
}
