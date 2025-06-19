using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class StopViolationDetector : MonoBehaviour
{
    [SerializeField] private float speedThresh = 2f;
    public GameManager gameManager;
    private float minSpeed = float.MaxValue;
    private bool inside = false;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "Body"){
            inside = true;
        }
    }
    void OnTriggerStay(Collider collider)
    {
        // Debug.Log(collider.name);
        if (collider.gameObject.name == "Body" && inside){
            float speed = collider.attachedRigidbody.velocity.magnitude;
            minSpeed = math.min(minSpeed, speed);
            if (minSpeed <= speedThresh){
                inside = false;
                gameManager.UpdateScore(+1, $"Stopped at a stop sign");
            }
        }
    }

    void OnTriggerExit(Collider collider){
        if (collider.gameObject.name == "Body" && inside){
            gameManager.UpdateScore(-3, $"Failed to Stop on Stop Sign (Slowed till {minSpeed.ToString("0.0")}, limit {speedThresh})");
        }
    }
}
