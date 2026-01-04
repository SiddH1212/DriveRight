using UnityEngine;
using System.Collections.Generic;

public abstract class GameManagerBase : MonoBehaviour
{
    public int score;
    public int fileCount = 0;
    public List<string> messageList = new List<string>();
    public abstract void ReportLightCross(string lightColor);
    public abstract void UpdateScore(int deltaScore, string message = "");
    public abstract void SaveAllViolationImages();
    public abstract void SaveImage(int idx);
}
