using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class GameManagerBase : MonoBehaviour
{
    public int score;
    public int fileCount = 0;
    public static bool SelectedshowText;
    public float elapsedTime;
    public float timeLimit = 100f;
    public List<string> messageList = new List<string>();
    public List<float> deltaScores = new();
    [NonSerialized] public List<LaneNode> currentPath;
    public bool gameplayActive;

    public abstract void ReportLightCross(string lightColor);
    public abstract void UpdateScore(int deltaScore, string message = "");
    public abstract void SaveAllViolationImages();
    public abstract void SaveImage(int idx);
}
