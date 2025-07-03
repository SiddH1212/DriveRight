using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System;

[ExecuteInEditMode]
public class RoadGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    public SplineContainer splineContainer;
    public MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public Texture2D texture;
    public float width = 10f;
    public int n_lanes = 2;
    public bool bidirectional = true;

    [Header("Mesh UV")]
    [SerializeField] private float uvOffset = 0f, lineCloseness = 0.01f;
    public float step = 0.01f;

    [Header("Speed Bumps")]
    [SerializeField] private int minBumps = 0;
    [SerializeField] private int maxBumps = 1;
    [SerializeField] private int bumpWidth = 2;      // how many segments each bump spans
    [SerializeField] private float bumpHeight = 0.2f;  // vertical rise

    // Generated points
    [HideInInspector] public List<float3> pIn = new List<float3>();
    [HideInInspector] public List<float3> pOut = new List<float3>();
    [HideInInspector] public List<Vector3> tangents = new List<Vector3>();
    [HideInInspector] public List<List<float3>> pLanes = new List<List<float3>>();
    [HideInInspector] public List<Vector3> laneTangents = new List<Vector3>();


    // Bump indices along the pIn/pOut lists
    private List<int> bumpCenters = new List<int>();

    private void Awake()    => GenerateAndBuild();
    private void OnEnable() => Spline.Changed += OnSplineChanged;
    private void OnDisable()=> Spline.Changed -= OnSplineChanged;

    private void OnSplineChanged(Spline s, int idx, SplineModification mod)
    {
        GenerateAndBuild();
    }

    private void GenerateAndBuild()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter      = GetComponent<MeshFilter>();
        meshCollider    = GetComponent<MeshCollider>();

        GeneratePoints();
        PickRandomBumps();
        BuildMesh();
    }

    private void GeneratePoints()
    {
        pIn.Clear();
        pOut.Clear();
        tangents.Clear();

        if (splineContainer == null) return;

        // sample every `step` along each spline
        foreach (var spline in splineContainer.Splines)
        {
            float3 posF, tanF, upF;
            for (float t = 0; t <= 1f; t += step)
            {
                spline.Evaluate(t, out posF, out tanF, out upF);

                // compute left/right edge in world space
                Vector3 worldPos = splineContainer.transform.TransformPoint((Vector3)posF);
                Vector3 worldTangent = splineContainer.transform.TransformDirection((Vector3)tanF).normalized;
                Vector3 worldUp = splineContainer.transform.up;
                Vector3 right = Vector3.Cross(worldTangent, worldUp).normalized * width;

                pIn.Add(worldPos + right);
                pOut.Add(worldPos - right);
                tangents.Add(worldTangent);
            }
        }

        GenerateLanePoints();
    }
    
    private void GenerateLanePoints()
    {
        for (int i = 0; i < pIn.Count; i += 5)
        {
            List<float3> lanePoints = new List<float3>();
            laneTangents.Add(tangents[i]);

            if (!bidirectional)
            {
                for (int j = 0; j < n_lanes; j++)
                {
                    lanePoints.Add(Vector3.Lerp(pIn[i], pOut[i], 1f / (2 * n_lanes) + (float)j / n_lanes));
                }
            }
            else
            {
                for (int j = 0; j < 2 * n_lanes; j++)
                {
                    lanePoints.Add(Vector3.Lerp(pIn[i], pOut[i], 1f / (4 * n_lanes) + (float)j / (2 * n_lanes)));
                }
            }

            pLanes.Add(lanePoints);
        }
    }


    private void PickRandomBumps()
    {
        bumpCenters.Clear();
        if (pIn.Count == 0) return;

        int numBumps = UnityEngine.Random.Range(minBumps, maxBumps + 1);
        for (int i = 0; i < numBumps; i++)
        {
            int centerIdx = UnityEngine.Random.Range(0 + bumpWidth, pIn.Count - bumpWidth);
            bumpCenters.Add(centerIdx);
        }
    }

    private void BuildMesh()
    {
        var mesh  = new Mesh();
        var verts = new List<Vector3>();
        var tris  = new List<int>();
        var uvs   = new List<Vector2>();

        float runningOffset = uvOffset;
        int count = pIn.Count;

        for (int i = 0; i < count - 1; i++)
        {
            // convert world to local mesh coords
            Vector3 w1 = transform.InverseTransformPoint((Vector3)pIn[i]);
            Vector3 w2 = transform.InverseTransformPoint((Vector3)pOut[i]);
            Vector3 w3 = transform.InverseTransformPoint((Vector3)pIn[i+1]);
            Vector3 w4 = transform.InverseTransformPoint((Vector3)pOut[i+1]);

            // apply bump if this segment is within bumpWidth of a bump center
            // float bumpOffset = 0f;
            float bumpOffset1 = 0f, bumpOffset2 = 0f;
            foreach (int center in bumpCenters)
            {
                float rel = (i - (center - bumpWidth)) / (2f * bumpWidth);
                if (rel >= 0f && rel <= 2f)
                {
                    float h = bumpHeight;
                    bumpOffset1 = (Mathf.Cos(rel * Mathf.PI - Mathf.PI) + 1f) * h/2;
                }

                float relNext = (i + 1 - (center - bumpWidth)) / (2f * bumpWidth);
                if (relNext >= 0f && relNext <= 2f)
                {
                    float h = bumpHeight;
                    bumpOffset2 = (Mathf.Cos(relNext * Mathf.PI - Mathf.PI) + 1f) * h/2;
                }


            }
            if (bumpOffset1 > 0f)
            {
                w1.y += bumpOffset1;
                w2.y += bumpOffset1;
            }
            if (bumpOffset2 > 0f)
            {
                w3.y += bumpOffset2;
                w4.y += bumpOffset2;
            }

            verts.AddRange(new[]{ w1, w2, w3, w4 });

            // UV along length
            float dist = Vector3.Distance(w1, w3) * lineCloseness;
            float nextU = runningOffset + dist;
            uvs.Add(new Vector2(runningOffset, 0));
            uvs.Add(new Vector2(runningOffset, 1));
            uvs.Add(new Vector2(nextU,       0));
            uvs.Add(new Vector2(nextU,       1));

            // triangles
            int b = i * 4;
            tris.AddRange(new[]{ b, b+2, b+3,   b+1, b, b+3 });

            runningOffset = nextU;
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        

        // ensure material & texture
        var rend = meshFilter.GetComponent<Renderer>();
        if (rend.sharedMaterial == null)
            rend.sharedMaterial = Resources.Load<Material>("RoadMaterial");
        if (texture != null)
            rend.sharedMaterial.mainTexture = texture;

        gameObject.layer = LayerMask.NameToLayer("Ground");
    }

    // void OnDrawGizmosSelected()
    // {
    //     // visualize bump centers in scene view
    //     Gizmos.color = Color.red;
    //     foreach (int center in bumpCenters)
    //     {
    //         if (center >= 0 && center < pIn.Count)
    //             Gizmos.DrawWireSphere(pIn[center], 1f);
    //     }
    // }
}