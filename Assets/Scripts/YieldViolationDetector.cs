using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class YieldViolationDetector : MonoBehaviour
{
    [SerializeField] private float speedThresh = 6f;
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
        if (collider.gameObject.name == "Body" && inside){
            float speed = collider.attachedRigidbody.velocity.magnitude;
            minSpeed = math.min(minSpeed, speed);
            if (minSpeed <= speedThresh){
                inside = false;
                gameManager.UpdateScore(+10, $"Slowed at a Yield sign");
            }
        }
    }

    void OnTriggerExit(Collider collider){
        if (collider.gameObject.name == "Body" && inside){
            gameManager.UpdateScore(-5, $"Failed to Slow on Yield Sign (Slowed till {minSpeed.ToString("0.0")}, limit {speedThresh})");
        }
    }
}
