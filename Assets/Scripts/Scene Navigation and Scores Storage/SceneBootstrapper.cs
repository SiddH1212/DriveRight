using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

public class SceneModeBootstrapper : MonoBehaviour
{
    [SerializeField] private GameObject vrRig;
    [SerializeField] private Camera nonVrCamera;
    [SerializeField] private GameObject androidControls;
    [SerializeField] private GameObject Gear;

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
        androidControls?.SetActive(true);
        Gear?.SetActive(false);
    }
}
