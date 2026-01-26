using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource idleSound;
    public AudioSource runningSound;

    [Header("Tuning")]
    public float maxSpeed = 220f;
    public float smoothing = 5f;
    public float runningMaxVolume = 0.5f;
    public float runningMaxPitch = 1.25f;
    public float idleMaxVolume = 0.5f;

    [Header("Limiter")]
    public float limiterSound = .4f;
    public float limiterFrequency = 2.5f;
    public float limiterEngage = 0.8f;

    private Vector3 prevPos;
    private float displayedSpeed;
    private float revLimiter;
    void Start()
    {
        prevPos = transform.position;

        // make sure idle/running sounds are silent and looping
        if (idleSound) { idleSound.volume = 0; idleSound.loop = true; idleSound.Play(); }
        if (runningSound) { runningSound.volume = 0; runningSound.loop = true; runningSound.Play(); }
    }

    void LateUpdate()
    {
        // ignore frames where nothing moves or game is paused
        // if (Time.timeScale == 0f) return;

        Vector3 delta = transform.position - prevPos;
        float rawSpeed = Vector3.Project(delta, transform.forward).magnitude * 3.6f / Time.unscaledDeltaTime;

        if (Time.timeScale > 0f)
        {
            displayedSpeed = Mathf.Lerp(displayedSpeed, rawSpeed, Time.unscaledDeltaTime * smoothing);
            UpdateEngineSounds(displayedSpeed / maxSpeed);
        }
        else
        {
            // first few frames of no movement — keep sounds muted
            if (idleSound) idleSound.volume = 0f;
            if (runningSound) runningSound.volume = 0f;
        }
        prevPos = transform.position;
    }

    void UpdateEngineSounds(float speedRatio)
    {
        speedRatio = Mathf.Clamp01(speedRatio);

        if (speedRatio > limiterEngage)
            revLimiter = (Mathf.Sin(Time.time * limiterFrequency) + 1f) * limiterSound * (speedRatio - limiterEngage);
        else
            revLimiter = 0f;

        if (idleSound)
            idleSound.volume = Mathf.Lerp(idleSound.volume, Mathf.Lerp(0.2f, idleMaxVolume, 1 - speedRatio), Time.deltaTime * 5f);

        if (runningSound)
        {
            runningSound.volume = Mathf.Lerp(runningSound.volume, Mathf.Lerp(0.2f, runningMaxVolume, speedRatio), Time.deltaTime * 5f);
            runningSound.pitch = Mathf.Lerp(runningSound.pitch, Mathf.Lerp(0.5f, runningMaxPitch, speedRatio) + revLimiter, Time.deltaTime * 5f);
        }
    }
}
