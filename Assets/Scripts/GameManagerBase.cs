using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class GameManagerBase : MonoBehaviour
{
    public int score;
    public static bool SelectedshowText = true;

    public float elapsedTime;
    public float timeLimit = 100f;

    public bool gameplayActive;
    public bool graceActive;

    [NonSerialized] public List<LaneNode> currentPath;

    public abstract void ReportLightCross(string lightColor);
    public abstract void UpdateScore(int deltaScore, string message = "");
    public abstract void SaveAllViolationImages();

    [System.Serializable]
    public class ViolationRecord
    {
        public string imagePath;     // SOURCE OF TRUTH
        public string message;
        public int deltaScore;
        public float time;
    }

    public List<ViolationRecord> violations = new();
    public int ViolationCount => violations.Count;

    public ViolationRecord GetViolation(int index)
    {
        return violations[index];
    }
}
