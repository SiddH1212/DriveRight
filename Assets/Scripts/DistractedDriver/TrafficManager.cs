using UnityEngine;
using System.Collections;

public class TrafficManager : MonoBehaviour
{
    public HaloOnlyTrafficLightController lightA1;
    public HaloOnlyTrafficLightController lightA2;
    public HaloOnlyTrafficLightController lightB1;
    public HaloOnlyTrafficLightController lightB2;
    public HaloOnlyTrafficLightController lightC1;
    public HaloOnlyTrafficLightController lightC2;
    public HaloOnlyTrafficLightController lightD1;
    public HaloOnlyTrafficLightController lightD2;


    void Start()
    {
        StartCoroutine(RunTrafficLights());
        lightC1.SetRed();
        lightC2.SetRed();  
        lightD1.SetRed();
        lightD2.SetRed();
    }

    IEnumerator RunTrafficLights()
    {
        // t = 0s — Player's light is green, opposite direction is red
        lightA1.SetGreen();
        lightA2.SetGreen();
        lightB1.SetRed();
        lightB2.SetRed();
        Debug.Log("1");
        yield return new WaitForSeconds(7f);

        // t = 7s — Player's light turns yellow
        lightA1.SetYellow();
        lightA2.SetYellow();
        Debug.Log("2");
        yield return new WaitForSeconds(3f); // now t = 10s

        // t = 10s — Player's light is red, opposite turns green
        lightA1.SetRed();
        lightA2.SetRed();
        lightB1.SetGreen();
        lightB2.SetGreen();
        Debug.Log("3");
        yield return new WaitForSeconds(10f); // now t = 20s
        lightA1.SetGreen();
        lightA2.SetGreen();
        lightB1.SetRed();
        lightB2.SetRed();
        Debug.Log("4");
        // t = 20s — Turn off red lights (optional reset)
        // lightA1.TurnOffAll();
        // lightA2.TurnOffAll();
        // lightB1.TurnOffAll();
        // lightB2.TurnOffAll();
    }
}
