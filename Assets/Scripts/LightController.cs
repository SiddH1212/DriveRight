using TMPro;
using UnityEngine;

public class LightController : MonoBehaviour
{
    private Renderer redRenderer, yellowRenderer, greenRenderer;
    private TextMeshPro timer;
    private bool isRed = false;
    private bool isYellow = false;
    private bool isGreen = false;
    void Awake()
    {
        redRenderer = transform.Find("Traffic_Lights/Red_Light").GetComponent<Renderer>();
        yellowRenderer = transform.Find("Traffic_Lights/Yellow_Light").GetComponent<Renderer>();
        greenRenderer = transform.Find("Traffic_Lights/Green_Light").GetComponent<Renderer>();
        timer = transform.Find("Timer").Find("Display").GetComponent<TextMeshPro>();
        if (timer == null) Debug.Log("Timer not found");
    }

    public void SetLightState(bool red, bool yellow, bool green)
    {
        isRed = red;
        isYellow = yellow;
        isGreen = green;
        if (red) redRenderer.material.EnableKeyword("_EMISSION");
        else redRenderer.material.DisableKeyword("_EMISSION");

        if (yellow) yellowRenderer.material.EnableKeyword("_EMISSION");
        else yellowRenderer.material.DisableKeyword("_EMISSION");

        if (green) greenRenderer.material.EnableKeyword("_EMISSION");
        else greenRenderer.material.DisableKeyword("_EMISSION");
    }

    public string GetState()
    {
        if (isRed) return "Red";
        else if (isYellow) return "Yellow";
        else return "Green";
    }
    
    public void UpdateTimerDisplay(string value)
    {
        if (timer != null)
        {
            timer.text = value;
        }
    }

}
