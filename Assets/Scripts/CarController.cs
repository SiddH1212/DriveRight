using System;
using UnityEngine;
using TMPro;


public class CarController : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbrakeForce;
    private bool isBraking;
    private float thresh = 1e-3f;
    public TextMeshProUGUI speedText;
    public GameManager gameManager;
    private CarIndicator carIndicator;

    // Car Params
    [SerializeField] private float motorForce, brakeForce, maxSteerAngle, steerSensitivity, steerReturn;

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;
    private Vector3 prevPos;
    void Start()
    {
        prevPos = transform.position;
        carIndicator = FindObjectOfType<CarIndicator>();
    }
    private void FixedUpdate() {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();

        Vector3 deltaPos = transform.position - prevPos;
        speedText.text = $"Speed: {MathF.Round(Vector3.Magnitude(deltaPos/Time.deltaTime))}";
        prevPos = transform.position;
    }

    private void GetInput()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");

        // Acceleration Input
        verticalInput = Input.GetAxis("Vertical");

        // Braking Input
        isBraking = Input.GetKey(KeyCode.Space);
        
        if (isBraking) carIndicator.TurnOnLights();
        else carIndicator.TurnOffLights();
    }

    private void HandleMotor() {
        // Rear wheel drive

        // frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        // frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        rearLeftWheelCollider.motorTorque = verticalInput * motorForce;
        rearRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking() {
        frontRightWheelCollider.brakeTorque = currentbrakeForce;
        frontLeftWheelCollider.brakeTorque = currentbrakeForce;
        // Rear wheel braking
        rearLeftWheelCollider.brakeTorque = currentbrakeForce;
        rearRightWheelCollider.brakeTorque = currentbrakeForce;
    }

    private void HandleSteering() {
        currentSteerAngle = Math.Min(maxSteerAngle, currentSteerAngle + steerSensitivity * horizontalInput);
        currentSteerAngle = Math.Max(-maxSteerAngle, currentSteerAngle);

        if (horizontalInput == 0){
            if (currentSteerAngle > thresh) currentSteerAngle -= currentSteerAngle/90 * steerReturn * steerSensitivity;
            else if (currentSteerAngle < -thresh) currentSteerAngle -= currentSteerAngle/90 * steerReturn * steerSensitivity;
            else currentSteerAngle = 0f;
        }   
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    } 

    private void UpdateWheels() {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform) {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Player collided with smth {collision.collider.gameObject.layer}");
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Vehicles")){
            gameManager.UpdateScore(-50, $"Collided with vechicle: {collision.collider.attachedRigidbody.gameObject.name}");
        }       
    }
}