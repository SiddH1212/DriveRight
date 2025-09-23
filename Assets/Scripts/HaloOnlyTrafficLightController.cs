using UnityEngine;

public class HaloOnlyTrafficLightController : MonoBehaviour
{
    [Header("Red Light")]
    public Renderer RedRenderer;
    public GameObject RedHalo;

    [Header("Yellow Light")]
    public Renderer YellowRenderer;
    public GameObject YellowHalo;

    [Header("Green Light")]
    public Renderer GreenRenderer;
    public GameObject GreenHalo;

    [Header("Materials")]
    public Material LightsOnMat;
    public Material LightsOffMat;

    public enum LightColor { Off, Red, Yellow, Green }

    private LightColor currentColor = LightColor.Off;

    public void SetRed() => SetLightState(LightColor.Red);
    public void SetYellow() => SetLightState(LightColor.Yellow);
    public void SetGreen() => SetLightState(LightColor.Green);
    public void TurnOffAll() => SetLightState(LightColor.Off);

    public LightColor GetCurrentColor() => currentColor;

    private void SetLightState(LightColor color)
    {
        currentColor = color;

        ApplyState(RedRenderer, RedHalo, color == LightColor.Red);
        ApplyState(YellowRenderer, YellowHalo, color == LightColor.Yellow);
        ApplyState(GreenRenderer, GreenHalo, color == LightColor.Green);
    }

    private void ApplyState(Renderer rend, GameObject halo, bool isOn)
    {
        if (rend != null)
            rend.material = isOn ? LightsOnMat : LightsOffMat;

        if (halo != null)
            halo.SetActive(isOn);
    }
}
