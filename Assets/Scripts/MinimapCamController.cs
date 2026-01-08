using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinimapCamController : MonoBehaviour
{
    public Transform player;
    public Transform destination;
    public PathRenderer pathRenderer;
    public PlayerMinimapArrow arrow;
    // public Button toggleButton;        
    // public TextMeshProUGUI buttonText;            
    Vector3 offset, midpoint;
    float camSize = 100f, camSizeOrig = 100f;
    bool follow = true;

    void Start()
    {
        offset = new Vector3(0f, 80f, 4f);
        transform.position = player.position + offset;

        midpoint = (player.position + destination.position) / 2;
        Vector3 diff = destination.position - player.position;
        camSize = Mathf.Max(diff.x, diff.z) / 2;

        // toggleButton.onClick.AddListener(ToggleFollow);
        // ToggleFollow();
        // UpdateButtonLabel();
    }

    void LateUpdate()
    {
        if (follow)
        {
            transform.position = player.position + offset;
        }
    }

    void ToggleFollow()
    {
        follow = !follow;
        float scale = camSize / camSizeOrig;

        if (!follow)
        {
            transform.position = midpoint + offset;
            GetComponent<Camera>().orthographicSize = camSize;
            pathRenderer.pathWidth *= scale;
            arrow.SetAbsoluteScale(scale);
        }
        else
        {
            GetComponent<Camera>().orthographicSize = camSizeOrig;
            pathRenderer.pathWidth /= scale;
            arrow.SetAbsoluteScale(1f);
        }

        // UpdateButtonLabel();
    }

//     void UpdateButtonLabel()
//     {
//         if (follow)
//             buttonText.text = "Switch to Overview";
//         else
//             buttonText.text = "Switch to Follow";
//     }
}
