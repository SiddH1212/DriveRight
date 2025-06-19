using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Car"){
            Debug.Log("Collision Detected");
        }
    }
}
