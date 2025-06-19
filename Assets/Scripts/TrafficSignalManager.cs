using UnityEngine;

public class TrafficSignalManager : MonoBehaviour
{
    [SerializeField] private int activePeriod = 10;
    [SerializeField] private int cooldownPeriod = 3;
    [SerializeField] private int stopPeriod = 8;

    private int period;
    private LightController[] allSignals;

    void Start()
    {
        period = stopPeriod + cooldownPeriod + activePeriod;
        allSignals = GetComponentsInChildren<LightController>();
    }

    void FixedUpdate()
    {
        int time = (int)Time.timeSinceLevelLoad % period;

        bool green = time < activePeriod;
        bool yellow = time >= activePeriod && time <= activePeriod + cooldownPeriod;
        bool red = time > activePeriod + cooldownPeriod;

        int redStartTime = activePeriod + cooldownPeriod;

        foreach (var signal in allSignals)
        {
            signal.SetLightState(red, yellow, green);

            if (red)
            {
                int remainingRedTime = period - time;
                signal.UpdateTimerDisplay(remainingRedTime.ToString("00"));
            }
            else
            {
                signal.UpdateTimerDisplay("00");  // clear timer if not red
            }
        }
    }
}
