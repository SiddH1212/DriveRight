using UnityEngine;

public class RedLightViolationDetector : MonoBehaviour
{
    public LightController lightController;
    public GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log(other.tag);
        if (other.CompareTag("Player"))
        {
            if (lightController.GetState() == "Red")
            {
                Debug.Log("Red light crossed!");
                gameManager.ReportLightCross("Red");
            }
            else if (lightController.GetState() == "Yellow"){
                Debug.Log("Yellow light crossed");
                gameManager.ReportLightCross("Yellow");
            }
            else{
                gameManager.ReportLightCross("Green");
            }
        }
    }
}
