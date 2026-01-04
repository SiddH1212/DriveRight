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

    void Awake()
    {
        if (AppMode.UseVR)
            StartCoroutine(EnableVR());
        else
            DisableVR();
    }

    IEnumerator EnableVR()
    {
        // Initialize XR loader if needed
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        }

        // Start XR subsystems
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        vrRig.SetActive(true);
        nonVrCamera.gameObject.SetActive(false);
        androidControls.SetActive(false);
    }

    void DisableVR()
    {
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
        }

        vrRig.SetActive(false);
        nonVrCamera.gameObject.SetActive(true);
        androidControls.SetActive(true);
    }
}
