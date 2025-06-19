using System.Collections;
using UnityEngine;

public class CarIndicator : MonoBehaviour
{
    [SerializeField] private GameObject leftIndicator, rightIndicator, leftArrow, rightArrow;

    public bool rightOn = false, leftOn = false;
    private Coroutine rightBlinkCoroutine = null, leftBlinkCoroutine = null;


    void Start()
    {
        leftIndicator.SetActive(false); leftArrow.SetActive(false);
        rightIndicator.SetActive(false); rightArrow.SetActive(false);
    }

    void Update()
    {
        bool rightIndicated = Input.GetKeyDown(KeyCode.Period);
        bool leftIndicated = Input.GetKeyDown(KeyCode.Comma);
        float horizontalInput = Input.GetAxis("Horizontal");

        bool stoppedIndicator = Input.GetKeyDown(KeyCode.Slash); // ||
                                                                 // (rightOn && (horizontalInput < 0)) ||
                                                                 // (leftOn && (horizontalInput > 0));

        if (rightIndicated && !rightOn)
        {
            rightOn = true;
            rightBlinkCoroutine = StartCoroutine(Blink(rightIndicator, rightArrow));
        }
        else if (leftIndicated && !leftOn)
        {
            leftOn = true;
            leftBlinkCoroutine = StartCoroutine(Blink(leftIndicator, leftArrow));
        }
        else if (stoppedIndicator && (rightOn || leftOn))
        {
            TurnOffIndicators();
        }
    }

    IEnumerator Blink(GameObject indicator, GameObject indicationArrow)
    {
        while (true)
        {
            indicator.SetActive(true);
            indicationArrow.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            indicator.SetActive(false);
            indicationArrow.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TurnOffIndicators()
    {
        if (rightBlinkCoroutine != null) StopCoroutine(rightBlinkCoroutine);
        if (leftBlinkCoroutine != null) StopCoroutine(leftBlinkCoroutine);

        rightOn = false;
        leftOn = false;

        leftIndicator.SetActive(false); leftArrow.SetActive(false);
        rightIndicator.SetActive(false); rightArrow.SetActive(false);
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
