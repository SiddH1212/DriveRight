using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheelVisualizer : MonoBehaviour
{
    [SerializeField] private InputActionReference steer;
    [SerializeField] private float maxWheelRotation = 200f;
    [SerializeField] private float returnSpeed = 8f;

    private float currentAngle = 0f;

    void OnEnable()
    {
        steer.action.Enable();
    }

    void OnDisable()
    {
        steer.action.Disable();
    }

    void Update()
    {
        Vector2 steerInput = steer.action.ReadValue<Vector2>();
        float targetAngle = steerInput.x * maxWheelRotation;

        // Smooth rotation
        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * returnSpeed
        );

        transform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
    }
}
