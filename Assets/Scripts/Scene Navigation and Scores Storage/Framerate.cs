using UnityEngine;

public class Framerate : MonoBehaviour
{
    private int badFrameCount = 0;
    private const int maxBadFrames = 20;
    private static bool exists = false;
    void Awake()
    {
        if (exists)
        {
            Destroy(gameObject);
            return;
        }
        exists = true;
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        float frameTimeMs = Time.deltaTime * 1000f;

        if (frameTimeMs > 33.5f)
            badFrameCount++;
        else
            badFrameCount = 0;

        if (badFrameCount >= maxBadFrames)
        {
            Application.targetFrameRate = 30;
            enabled = false;
        }
    }
}
