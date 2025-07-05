using UnityEngine;

public class PedestrianTrigger : MonoBehaviour
{
    public Animator pedestrianAnimator;
    public GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log(other.tag);
        if (other.CompareTag("Player"))
        {
            pedestrianAnimator.SetBool("isWalking", true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false) return;
        if (pedestrianAnimator.GetBool("isWalking") == true){
            gameManager.UpdateScore(-20, "Did not wait for the pedestrian to cross");
        }
        else{
            gameManager.UpdateScore(+5, "Waited for the pedestrian to cross");
        }
    }
}
