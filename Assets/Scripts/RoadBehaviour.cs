using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

[ExecuteInEditMode()]
public class RoadBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private SplineContainer splineContainer;
    private float ratio;
    public float step = 0.1f;
    private float3 position;
    private float3 tangent;
    private float3 upVect;
    public float width = 5;
    private float3 p1, p2;

    // Update is called once per frame
    public List <float3> pIn, pOut;

    void GeneratePoints(){
        for (ratio = 0; ratio <= 1; ratio += step){
            splineContainer.Evaluate(ratio, out position, out tangent, out upVect);
            float3 right = Vector3.Cross(tangent, upVect).normalized;
            p1 = position + (right * width);
            p2 = position + (-right * width);
            pIn.Append(p1);
            pOut.Append(p2);
        }
        Debug.Log(pIn.Count);
    }
    void Awake()
    {
        GeneratePoints();
    }
    void FixedUpdate()
    {
        // splineContainer.Evaluate(ratio, out position, out tangent, out upVect);
        // float3 right = Vector3.Cross(tangent, upVect).normalized;
        // p1 = position + (right * width);
        // p2 = position + (-right * width);

    }

    void OnDrawGizmos()
    {
        // Handles.matrix = transform.localToWorldMatrix;
        // for (int i = 0; i < pIn.Count; i++){
        //     // p1 = pIn[i];
        //     // p2 = pOut[i];
        //     Handles.SphereHandleCap(0, -splineContainer.transform.position + new Vector3 (p1.x, p1.y, p1.z), Quaternion.identity, 100f, EventType.Repaint);
        //     Handles.SphereHandleCap(0, -splineContainer.transform.position + new Vector3 (p2.x, p2.y, p2.z), Quaternion.identity, 100f, EventType.Repaint);
        // }
    }


}
