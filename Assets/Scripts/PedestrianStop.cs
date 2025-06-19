using UnityEngine;

public class PedestrianStop : MonoBehaviour
{
    public Animator pedestrianAnimator;
    void Start()
    {
        Debug.Log("Started");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log(other.tag);
        if (other.CompareTag("Pedestrian"))
        {
            pedestrianAnimator.SetBool("isWalking", false);
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         pedestrianAnimator.SetBool("isWalking", false);
    //     }
    // }
}
