using UnityEngine;
using UnityEngine.EventSystems;

public class ArcSteering : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    public RectTransform knob;        // The moving circle
    public RectTransform arcCenter;   // Center of the arc

    [Header("Arc Settings")]
    public float radius = 25f;
    public float minAngle = -90f;
    public float maxAngle = 90f;
    public float returnSpeed = 6f;

    [Header("Sensitivity")]
    public float deadZoneRadius = 20f;     // Ignore tiny movements
    public float maxInputRadius = 100f;    // Distance for full steering
    [Range(1f, 3f)]
    public float steeringExponent = 1.8f;  // Distance curve

    [Header("Steering Feel")]
    [Range(1f, 4f)]
    public float steeringResponse = 2.2f;  // Higher = less sensitive near center

    [Header("Smoothing")]
    public float steeringSmoothSpeed = 10f;

    [Header("Output")]
    [Range(-1f, 1f)]
    public float steeringValue; // -1 (left) → +1 (right)

    private bool isDragging;
    private float targetSteering;

    void Update()
    {
        // When released, smoothly return to center
        if (!isDragging)
        {
            targetSteering = 0f;
        }

        steeringValue = Mathf.Lerp(
            steeringValue,
            targetSteering,
            Time.deltaTime * steeringSmoothSpeed
        );

        UpdateKnobPosition(steeringValue);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 centerScreenPos =
            RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                arcCenter.position
            );

        Vector2 dir = eventData.position - centerScreenPos;
        float distance = dir.magnitude;

        // Dead zone
        if (distance < deadZoneRadius)
        {
            targetSteering = 0f;
            return;
        }

        // Distance normalization
        float distance01 = Mathf.Clamp01(
            (distance - deadZoneRadius) / (maxInputRadius - deadZoneRadius)
        );

        // Distance curve
        distance01 = Mathf.Pow(distance01, steeringExponent);

        // Angle calculation
        float angle = Vector2.SignedAngle(Vector2.up, dir);
        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        // Raw steering
        float rawSteering = -angle / maxAngle * distance01;

        // Final response curve (THIS is the key improvement)
        targetSteering =
            Mathf.Sign(rawSteering) *
            Mathf.Pow(Mathf.Abs(rawSteering), steeringResponse);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void UpdateKnobPosition(float value)
    {
        float angle = value * maxAngle;
        float rad = angle * Mathf.Deg2Rad;

        Vector2 pos = new Vector2(
            Mathf.Sin(rad),
            Mathf.Cos(rad)
        ) * radius;

        knob.anchoredPosition = pos;
    }
}
