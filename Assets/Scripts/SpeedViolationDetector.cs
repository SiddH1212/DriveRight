using TMPro;
using UnityEngine;

public class SpeedViolationDetector : MonoBehaviour
{
    private int speedLimit;
    public GameManager gameManager;
    public float minPenalty = 5f;
    public float maxPenalty = 100f;

    void Start()
    {
        speedLimit = int.Parse(transform.parent.Find("Value").GetComponent<TextMeshPro>().text);
        Debug.Log(speedLimit);
    }


    void OnTriggerEnter(Collider collider)
    {
        // Debug.Log(collider.name);
        if (collider.name != "Body") return;
        int speed = (int)collider.attachedRigidbody.velocity.magnitude;
        if (speed > speedLimit){
            gameManager.UpdateScore(-(int)Mathf.Min(minPenalty + (speed - speedLimit), maxPenalty), $"Overspeeding — driving above the speed limit of {speedLimit}");
        }
        else{
            gameManager.UpdateScore(10, $"Following speed limit");
        }
    }
}
