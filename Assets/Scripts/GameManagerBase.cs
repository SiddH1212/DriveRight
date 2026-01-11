using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class GameManagerBase : MonoBehaviour
{
    public int score;
    public int fileCount = 0; // legacy
    public static bool SelectedshowText;
    public float elapsedTime;
    public float timeLimit = 100f;
    public List<string> messageList = new();
    public List<float> deltaScores = new();
    [NonSerialized] public List<LaneNode> currentPath;
    public bool gameplayActive;
    public bool graceActive;

    public abstract void ReportLightCross(string lightColor);
    public abstract void UpdateScore(int deltaScore, string message = "");
    public abstract void SaveAllViolationImages();
    public abstract void SaveImage(int idx);

    [Serializable]
    public class ViolationRecord
    {
        public Texture2D image;          // RAM copy
        public string imagePath;         // disk path
        public string message;
        public int deltaScore;
        public float time;
    }

    public List<ViolationRecord> violations = new();
    public int ViolationCount => violations.Count;

    public ViolationRecord GetViolation(int index) => violations[index];
}
