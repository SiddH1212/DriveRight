using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class TimelineController : MonoBehaviour
{
    public Button textBack;
    public Button callNow;
    public Button ignore;
    public Button exit1;
    public Button exit2;
    public Button Play;
    public GameObject StartCanvas;
    public GameObject Canvas1;
    public GameObject Canvas2;
    public GameObject Canvas3;
    public PlayableDirector approachTimeline;
    public PlayableDirector crashTimeline;
    public PlayableDirector stopTimeline;
    public GameObject TrafficManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TrafficManager.SetActive(false);
        Canvas1.SetActive(false);
        Canvas2.SetActive(false);
        Canvas3.SetActive(false);
        StartCanvas.SetActive(true);
        Play.onClick.AddListener(onClickPlay);
        approachTimeline.stopped += OnApproachFinished;
        stopTimeline.stopped += OnStopFinished;
        crashTimeline.stopped += onCrashFinished;
    }
    public void onClickPlay()
    {
        approachTimeline.Play();
        StartCanvas.SetActive(false);
        TrafficManager.SetActive(true);
    }
    void OnApproachFinished(PlayableDirector director)
    {
        Time.timeScale = 0f;
        Canvas1.SetActive(true);
        textBack.onClick.AddListener(onClickWrong);
        callNow.onClick.AddListener(onClickWrong);
        ignore.onClick.AddListener(onClickCorrect);
    }
    void OnStopFinished(PlayableDirector director)
    {
        Canvas2.SetActive(true);
        exit1.onClick.AddListener(onClickExit);
    }
    public void onCrashFinished(PlayableDirector director)
    {
        Canvas3.SetActive(true);
        exit2.onClick.AddListener(onClickExit);
    }
    public void onClickWrong()
    {
        Time.timeScale = 1f;
        Canvas1.SetActive(false);
        crashTimeline.Play();
    }
    public void onClickCorrect()
    {
        Time.timeScale = 1f;
        Canvas1.SetActive(false);
        stopTimeline.Play();
    }
    public void onClickExit()
    {
        Application.Quit();
    }
    void OnDestroy()
    {
        approachTimeline.stopped -= OnApproachFinished;
        stopTimeline.stopped -= OnStopFinished;
        crashTimeline.stopped -= onCrashFinished;
    }
}
