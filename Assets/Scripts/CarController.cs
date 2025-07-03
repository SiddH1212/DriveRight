using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


public class CarController : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbrakeForce;
    // private bool isBraking;
    [SerializeField]
    private InputActionReference brake;
    private float thresh = 1e-3f;
    public TextMeshProUGUI speedText;
    public GameManager gameManager;

    // Car Params
    [SerializeField] private float motorForce, brakeForce, maxSteerAngle, steerSensitivity, steerReturn;

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private RectTransform speedNeedle;
    [SerializeField] private float needleMinAngle = 127f;
    [SerializeField] private float needleMaxAngle = -127f;
    [SerializeField] private float maxSpeed = 220f;
    private Vector3 prevPos;
    
    void Start()
    {
        brake.action.Enable();
        brake.action.started += BrakeOn;
        brake.action.canceled += BrakeOff;
        prevPos = transform.position;        
    }
    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        Vector3 deltaPos = transform.position - prevPos;
        float speed = Vector3.Magnitude(deltaPos / Time.deltaTime) * 3.6f; // convert to km/h
        speedText.text = $"Speed: {MathF.Round(speed)} km/h";
        prevPos = transform.position;
        UpdateSpeedometer(speed);
    }

    private void GetInput()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");

        // Acceleration Input
        verticalInput = Input.GetAxis("Vertical");

        // Braking Input
        // isBraking = Input.GetKey(KeyCode.Space);
    }
    private void BrakeOn(InputAction.CallbackContext context)
    {
        currentbrakeForce = brakeForce;
        Debug.Log("Breaking");
    }

    private void BrakeOff(InputAction.CallbackContext context)
    {
        currentbrakeForce = 0f;
        Debug.Log("Resuming");
    }

    private void HandleMotor()
    {
        // Rear wheel drive

        // frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        // frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        rearLeftWheelCollider.motorTorque = verticalInput * motorForce;
        rearRightWheelCollider.motorTorque = verticalInput * motorForce;
        // currentbrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontRightWheelCollider.brakeTorque = currentbrakeForce;
        frontLeftWheelCollider.brakeTorque = currentbrakeForce;
        // Rear wheel braking
        rearLeftWheelCollider.brakeTorque = currentbrakeForce;
        rearRightWheelCollider.brakeTorque = currentbrakeForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = Math.Min(maxSteerAngle, currentSteerAngle + steerSensitivity * horizontalInput);
        currentSteerAngle = Math.Max(-maxSteerAngle, currentSteerAngle);

        if (horizontalInput == 0)
        {
            if (currentSteerAngle > thresh) currentSteerAngle -= currentSteerAngle / 90 * steerReturn * steerSensitivity;
            else if (currentSteerAngle < -thresh) currentSteerAngle -= currentSteerAngle / 90 * steerReturn * steerSensitivity;
            else currentSteerAngle = 0f;
        }
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

        float currentSteerWheelRoation = -currentSteerAngle;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, currentSteerWheelRoation);
        steeringWheel.localRotation = Quaternion.Slerp(steeringWheel.localRotation, targetRotation, Time.deltaTime * 5f);
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
    void UpdateSpeedometer(float speed)
    {
        float clampedSpeed = Mathf.Clamp(speed, 0, maxSpeed);
        float t = clampedSpeed / maxSpeed;
        float angle = Mathf.Lerp(needleMinAngle, needleMaxAngle, t);
        speedNeedle.localRotation = Quaternion.Euler(0, 0, angle);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Player collided with smth {collision.collider.gameObject.layer}");
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Vehicles"))
        {
            gameManager.UpdateScore(-10, $"Collided with vechicle: {collision.collider.attachedRigidbody.gameObject.name}");
        }
    }
    private void OnDestroy()
    {
        brake.action.started -= BrakeOn;
        brake.action.canceled -= BrakeOff;
    }
}