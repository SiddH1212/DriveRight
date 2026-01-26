using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class SceneModeBootstrapper : MonoBehaviour
{
    [SerializeField] private GameObject vrRig;
    [SerializeField] private Camera nonVrCamera;
    [SerializeField] private GameObject androidControls;
    // [SerializeField] private GameObject Panel;
    [SerializeField] private GameObject VRPanel;
    [SerializeField] private GameObject Gear;
    [SerializeField] private GameObject steering;

    IEnumerator Start()
    {
        // Wait for XR + scene to be ready
        yield return null;
        yield return null;

        if (AppMode.UseVR)
            yield return EnableVR();
        else
            DisableVR();
    }

    IEnumerator EnableVR()
    {
        var xr = XRGeneralSettings.Instance;
        if (xr == null || xr.Manager == null)
        {
            Debug.LogWarning("XR not available");
            yield break;
        }

        if (xr.Manager.activeLoader == null)
        {
            yield return xr.Manager.InitializeLoader();
        }

        if (xr.Manager.activeLoader != null)
        {
            xr.Manager.StartSubsystems();
        }
        else
        {
            Debug.LogError("XR Loader failed to initialize");
            yield break;
        }

        // Enable / disable objects safely
        vrRig?.SetActive(true);
        if (nonVrCamera) nonVrCamera.gameObject.SetActive(false);
        androidControls?.SetActive(false);
        Gear?.SetActive(true);
        steering?.SetActive(true);
        VRPanel?.SetActive(true);
        // Panel?.SetActive(false);
    }

    void DisableVR()
    {
        var xr = XRGeneralSettings.Instance;
        if (xr != null && xr.Manager != null && xr.Manager.activeLoader != null)
        {
            xr.Manager.StopSubsystems();
        }

        vrRig?.SetActive(false);
        if (nonVrCamera) nonVrCamera.gameObject.SetActive(true);
        // if(IsControllerConnected())
        // {
        //     androidControls?.SetActive(false);
        //     Gear?.SetActive(true);
        //     steering?.SetActive(true);
        // }
        // else
        // {
        //     androidControls?.SetActive(true);
        //     Gear?.SetActive(false);
        //     steering?.SetActive(false);
        // }
        androidControls?.SetActive(true);
        Gear?.SetActive(false);
        steering?.SetActive(false);
        VRPanel?.SetActive(false);
        // Panel?.SetActive(true);
    }
      public bool IsControllerConnected()
    {
        return InputMode.IsControllerConnected();
    }
}
