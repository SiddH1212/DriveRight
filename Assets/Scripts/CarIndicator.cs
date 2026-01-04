using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarIndicator : MonoBehaviour
{
    [SerializeField] private GameObject leftIndicator, rightIndicator, leftArrow, rightArrow;
    [SerializeField] private InputActionReference left, right;

    public bool leftOn = false;
    public bool rightOn = false;

    private Coroutine leftBlinkCoroutine;
    private Coroutine rightBlinkCoroutine;

    void Start()
    {
        left.action.Enable();
        right.action.Enable();

        left.action.started += OnLeftPressed;
        right.action.started += OnRightPressed;

        TurnOffVisuals();
    }

    private void OnDestroy()
    {
        left.action.started -= OnLeftPressed;
        right.action.started -= OnRightPressed;
    }

    /* ---------------- INPUT ---------------- */

    private void OnLeftPressed(InputAction.CallbackContext ctx)
    {
        if (leftOn)
        {
            TurnOffIndicators();
        }
        else
        {
            TurnOnLeft();
        }
    }

    private void OnRightPressed(InputAction.CallbackContext ctx)
    {
        if (rightOn)
        {
            TurnOffIndicators();
        }
        else
        {
            TurnOnRight();
        }
    }

    /* ---------------- STATE ---------------- */

    private void TurnOnLeft()
    {
        TurnOffIndicators();

        leftOn = true;
        leftBlinkCoroutine = StartCoroutine(
            Blink(leftIndicator, leftArrow)
        );
    }

    private void TurnOnRight()
    {
        TurnOffIndicators();

        rightOn = true;
        rightBlinkCoroutine = StartCoroutine(
            Blink(rightIndicator, rightArrow)
        );
    }

    public void TurnOffIndicators()
    {
        if (leftBlinkCoroutine != null)
        {
            StopCoroutine(leftBlinkCoroutine);
            leftBlinkCoroutine = null;
        }

        if (rightBlinkCoroutine != null)
        {
            StopCoroutine(rightBlinkCoroutine);
            rightBlinkCoroutine = null;
        }

        leftOn = false;
        rightOn = false;

        TurnOffVisuals();
    }

    private void TurnOffVisuals()
    {
        leftIndicator.SetActive(false);
        leftArrow.SetActive(false);
        rightIndicator.SetActive(false);
        rightArrow.SetActive(false);
    }

    /* ---------------- BLINK ---------------- */

    private IEnumerator Blink(GameObject indicator, GameObject arrow)
    {
        while (true)
        {
            indicator.SetActive(true);
            arrow.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            indicator.SetActive(false);
            arrow.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
