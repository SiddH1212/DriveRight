using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MinimapRenderer : MonoBehaviour{
    [SerializeField] 
    public RenderTexture minimapCam;
    public RawImage targetImage;

    void FixedUpdate(){
        // targetImage.texture = minimapCam.targetTexture;
        // targetImage.material.SetTexture("Nw", minimapCam.targetTexture);
        targetImage.texture = minimapCam;

    }


}