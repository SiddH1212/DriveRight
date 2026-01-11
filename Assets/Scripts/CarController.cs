using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    /* ---------------- INPUT ---------------- */

    [SerializeField] private InputActionReference steer;
    [SerializeField] private InputActionReference accelerate;
    [SerializeField] private InputActionReference brake;
    [SerializeField] private InputActionReference toggleGear;
    [SerializeField] private InputActionReference MapToggle;

    /* ---------------- STATE ---------------- */

    private float horizontalInput;
    private float verticalInput;
    private float currentSteerAngle;
    private float currentBrakeForce;

    private bool isReverse = false; // false = Drive, true = Reverse

    /* ---------------- UI ---------------- */

    public TextMeshProUGUI speedText;
    public GameManagerBase gameManager;
    [SerializeField] private GameObject minimap;
    private bool minimapVisible = true;
    [SerializeField] private TextMeshProUGUI Gear;

    /* ---------------- CAR PARAMS ---------------- */

    [SerializeField] private float motorForce = 1500f;
    [SerializeField] private float brakeForce = 3000f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float steerSensitivity = 1.5f;
    [SerializeField] private float steerReturn = 4f;

    /* ---------------- WHEELS ---------------- */

    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    /* ---------------- VISUALS ---------------- */

    [SerializeField] private Transform steeringWheel;
    [SerializeField] private RectTransform speedNeedle;
    [SerializeField] private float needleMinAngle = 127f;
    [SerializeField] private float needleMaxAngle = -127f;
    [SerializeField] private float maxSpeed = 220f;

    private Vector3 prevPos;
    private const float steerDeadzone = 0.05f;

    /* ===================== UNITY ===================== */
    public GameObject minimapCam;

    void Start()
    {
        steer.action.Enable();
        accelerate.action.Enable();
        brake.action.Enable();
        toggleGear.action.Enable();
        MapToggle.action.Enable();
        brake.action.started += BrakeOn;
        brake.action.canceled += BrakeOff;
        toggleGear.action.started += ToggleGear;
        MapToggle.action.started += ToggleMinimap;
        minimapCam.SetActive(minimapVisible);
        prevPos = transform.position;
        isReverse = false;
        speedText.font = LanguageTranslator.Instance.GetFont();

    }

    void FixedUpdate()
    {
        if(!gameManager.gameplayActive)
            return;
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        UpdateSpeed();
        UpdateUI();
    }

    private void OnDestroy()
    {
        steer.action.Disable();
        accelerate.action.Disable();
        toggleGear.action.Disable();
        MapToggle.action.Disable();
        brake.action.started -= BrakeOn;
        brake.action.canceled -= BrakeOff;
        toggleGear.action.started -= ToggleGear;
        MapToggle.action.started -= ToggleMinimap;
    }

    /* ===================== INPUT ===================== */

    private void GetInput()
    {
        // Vector2 steerInput = steer.action.ReadValue<Vector2>();
        // horizontalInput = Mathf.Abs(steerInput.x) < steerDeadzone ? 0f : steerInput.x;
        // float steerInput = steer.action.ReadValue<float>();
       float steerInput = SimpleInput.GetAxis("Horizontal");

        // Fallback if SimpleInput returns 0 (no touch input)
        if (Mathf.Approximately(steerInput, 0f))
        {
            steerInput = Input.GetAxis("Horizontal");
        }

        horizontalInput = Mathf.Abs(steerInput) < steerDeadzone ? 0f : steerInput;

        verticalInput = accelerate.action.ReadValue<float>();
    }

    private void ToggleGear(InputAction.CallbackContext ctx)
    {
        // float speed = (transform.position - prevPos).magnitude / Time.deltaTime;

        // // Prevent gear switch at speed
        // if (speed > 1.5f) return;

        isReverse = !isReverse;
        Gear.text=isReverse ? "R" : "D";
    }

    private void BrakeOn(InputAction.CallbackContext ctx)
    {
        currentBrakeForce = brakeForce;
    }

    private void BrakeOff(InputAction.CallbackContext ctx)
    {
        currentBrakeForce = 0f;
    }

    /* ===================== MOTOR ===================== */
    private void ToggleMinimap(InputAction.CallbackContext ctx)
    {
        minimapVisible = !minimapVisible;
        minimap.SetActive(minimapVisible);
        minimapCam.SetActive(minimapVisible);
        Debug.Log(minimapVisible ? "Minimap ON" : "Minimap OFF");
    }
    private void HandleMotor()
    {
        float gearDirection = isReverse ? -1f : 1f;
        float motorInput = verticalInput * gearDirection;

        rearLeftWheelCollider.motorTorque = motorInput * motorForce;
        rearRightWheelCollider.motorTorque = motorInput * motorForce;

        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }

    /* ===================== STEERING ===================== */

    private void HandleSteering()
    {
        currentSteerAngle += steerSensitivity * horizontalInput;
        currentSteerAngle = Mathf.Clamp(currentSteerAngle, -maxSteerAngle, maxSteerAngle);

        if (horizontalInput == 0f)
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, 0f, Time.deltaTime * steerReturn);

        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -currentSteerAngle);
        steeringWheel.localRotation = Quaternion.Slerp(
            steeringWheel.localRotation,
            targetRotation,
            Time.deltaTime * 5f
        );
    }

    /* ===================== VISUALS ===================== */

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheel, Transform wheelTransform)
    {
        wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private void UpdateSpeed()
    {
        Vector3 deltaPos = transform.position - prevPos;
        float speed = deltaPos.magnitude / Time.deltaTime * 3.6f;
        prevPos = transform.position;

        // speedText.text = $"Speed: {Mathf.Round(speed)} km/h";
        UpdateSpeedometer(speed);
    }

    private void UpdateSpeedometer(float speed)
    {
        float t = Mathf.Clamp01(speed / maxSpeed);
        float angle = Mathf.Lerp(needleMinAngle, needleMaxAngle, t);
        speedNeedle.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /* ===================== COLLISION ===================== */

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Vehicles"))
        {
            gameManager.UpdateScore(
                -10,
                $"Collided with vehicle: {collision.collider.attachedRigidbody.name}"
            );
        }
    }
    /// <summary>
    /// Called by UISwitcher (true = Reverse, false = Drive)
    /// </summary>
    public void SetReverseFromUIToggle(bool reverse)
    {
        isReverse = reverse;
        Debug.Log(isReverse ? "Gear (UI): REVERSE" : "Gear (UI): DRIVE");
    }
    private void UpdateUI()
    {
        // string speedTranslated = LanguageTranslator.Instance.Translate("Speed");
        // string timeTranslated = LanguageTranslator.Instance.Translate("Time Elapsed");
        // string timeLimitTranslated = LanguageTranslator.Instance.Translate("Time Limit");
        string timeTranslated = LanguageTranslator.Instance.Translate("Time");
        float remainingTime = Mathf.Max(
            0f,
            gameManager.timeLimit - gameManager.elapsedTime
        );

        // string formattedElapsed = TimeSpan.FromSeconds(gameManager.elapsedTime).ToString(@"hh\:mm\:ss");
        // string formattedLimit = TimeSpan.FromSeconds(gameManager.timeLimit).ToString(@"hh\:mm\:ss");
        string formattedTime = FormatTime(remainingTime);
        // speedText.text = $" {timeTranslated}: {formattedElapsed}\n {timeLimitTranslated}: {formattedLimit}";
        speedText.text = $"{timeTranslated}: {formattedTime}";
    }
    private string FormatTime(float seconds)
    {
        return TimeSpan
            .FromSeconds(seconds)
            .ToString(@"mm\:ss");
    }

}
