using System.Collections;
using UnityEngine;

public class NPCIndicator : MonoBehaviour
{
    [SerializeField] private GameObject leftIndicator, rightIndicator;

    public bool rightOn = false, leftOn = false;
    private Coroutine rightBlinkCoroutine = null, leftBlinkCoroutine = null;


    void Start()
    {
        leftIndicator.SetActive(false);
        rightIndicator.SetActive(false);
    }

    void Update()
    {
        bool rightIndicated = false, leftIndicated = false, stoppedIndicator = false;
        // bool rightIndicated = Input.GetKeyDown(KeyCode.Period);
        // bool leftIndicated = Input.GetKeyDown(KeyCode.Comma);

        // bool stoppedIndicator = Input.GetKeyDown(KeyCode.Slash); // ||
        // (rightOn && (horizontalInput < 0)) ||
        // (leftOn && (horizontalInput > 0));

        if (rightIndicated && !rightOn)
        {
            rightOn = true;
            rightBlinkCoroutine = StartCoroutine(Blink(rightIndicator));
        }
        else if (leftIndicated && !leftOn)
        {
            leftOn = true;
            leftBlinkCoroutine = StartCoroutine(Blink(leftIndicator));
        }
        else if (stoppedIndicator && (rightOn || leftOn))
        {
            TurnOffIndicators();
        }
    }

    IEnumerator Blink(GameObject indicator)
    {
        while (true)
        {
            indicator.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            indicator.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TurnOffIndicators()
    {
        if (rightBlinkCoroutine != null) StopCoroutine(rightBlinkCoroutine);
        if (leftBlinkCoroutine != null) StopCoroutine(leftBlinkCoroutine);

        rightOn = false;
        leftOn = false;

        leftIndicator.SetActive(false);
        rightIndicator.SetActive(false);
    }

    public void TurnOnLights()
    {
        if (!leftOn)  leftIndicator.SetActive(true);
        if (!rightOn) rightIndicator.SetActive(true);
    }

    public void TurnOffLights()
    {
        if (!leftOn)  leftIndicator.SetActive(false);
        if (!rightOn) rightIndicator.SetActive(false);
    }
}
