using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using System;

[ExecuteInEditMode]
public class RoadGeneratorCopy : MonoBehaviour
{
    public SplineContainer splineContainer;
    [SerializeField] private float uvOffset = 0f, lineCloseness = 0.01f;
    [SerializeField] private Texture2D texture;
    public float step = 0.01f;
    public MeshFilter meshFilter;
    public float width = 10f;
    public int n_lanes = 2;
    public bool bidirectional = true;

    [HideInInspector] public List<float3> pIn = new List<float3>();
    [HideInInspector] public List<float3> pOut = new List<float3>();
    [HideInInspector] public List<List<float3>> pLanes = new List<List<float3>>();
    [HideInInspector] public List<Vector3> tangents = new List<Vector3>();
    [HideInInspector] public List<Vector3> laneTangents = new List<Vector3>();

    private void Awake() => GenerateAndBuild();
    private void OnEnable() => Spline.Changed += OnSplineChanged;
    private void OnDisable() => Spline.Changed -= OnSplineChanged;

    private void OnSplineChanged(Spline s, int idx, SplineModification mod)
    {
        // Debug.Log("Changed");
        GenerateAndBuild();
    }

    private void GenerateAndBuild()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
        GeneratePoints();
        BuildMesh();
    }

    private void GeneratePoints()
    {
        pIn.Clear();
        pOut.Clear();
        tangents.Clear();
        // float step_dist = 40;
        if (splineContainer == null) Debug.Log("cont null");
        if (splineContainer.Splines == null) Debug.Log("splines null");
        for (int si = 0; si < splineContainer.Splines.Count; si++)
        {
            float3 posF, tanF, upF;
            // float step_ = step * (int)splineContainer.Splines[si].GetLength()/(int)splineContainer.Splines[si].GetLength();
            float step_ = step;
            // Debug.Log(splineContainer.transform.position + "Length "+ splineContainer.Splines[si].GetLength());
            for (float t = 0; t <= 1f;t+=step_)
            {
                t = (float)Math.Round(t, 2);
                splineContainer.Evaluate(si, t, out posF, out tanF, out upF);

                Vector3 localPos = splineContainer.transform.InverseTransformPoint((Vector3)posF);
                Vector3 localTangent = splineContainer.transform.InverseTransformDirection((Vector3)tanF).normalized;
                Vector3 localUp = splineContainer.transform.InverseTransformDirection((Vector3)upF).normalized;

                Vector3 localRight = -Vector3.Cross(localTangent, localUp).normalized;

                Vector3 inLocal = localPos - localRight * width;
                Vector3 outLocal = localPos + localRight * width;

                Vector3 worldIn = splineContainer.transform.TransformPoint(inLocal);
                Vector3 worldOut = splineContainer.transform.TransformPoint(outLocal);
                Vector3 worldTangent = splineContainer.transform.TransformDirection(localTangent);

                pIn.Add(worldIn);
                pOut.Add(worldOut);
                tangents.Add(worldTangent);

                // if (t < 1 && t + step_ > 1) t = 1;
                // else t += step_;

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

    private void BuildMesh()
    {
        Mesh m = new Mesh();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();

        float runningOffset = uvOffset;
        int count = pIn.Count;

        for (int i = 0; i < count-1; i++)
        {
            Vector3 w1 = splineContainer.transform.InverseTransformPoint(pIn[i]);
            Vector3 w2 = splineContainer.transform.InverseTransformPoint(pOut[i]);
            Vector3 w3 = splineContainer.transform.InverseTransformPoint(pIn[(i + 1) % count]);
            Vector3 w4 = splineContainer.transform.InverseTransformPoint(pOut[(i + 1) % count]);

            verts.AddRange(new[] { w1, w2, w3, w4 });

            float dist = Vector3.Distance(w1, w3) * lineCloseness;
            float nextU = runningOffset + dist;

            uvs.Add(new Vector2(runningOffset, 0));
            uvs.Add(new Vector2(runningOffset, 1));
            uvs.Add(new Vector2(nextU, 0));
            uvs.Add(new Vector2(nextU, 1));

            int baseV = i * 4;
            tris.AddRange(new[] { baseV, baseV + 2, baseV + 3, baseV + 3, baseV + 1, baseV });

            runningOffset = nextU;
        }

        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals();
        m.RecalculateBounds();

        meshFilter.sharedMesh = m;
        if (texture == null)
        {
            // texture = Resources.Load<Texture2D>($"road_{n_lanes}{(bidirectional ? "_bi" : "")}");
            texture = Resources.Load<Texture2D>("road_1");
        }

        if (texture == null) Debug.Log("Texture still null " + $"road_{n_lanes}{(bidirectional ? "_bi" : "")}");
        var renderer = meshFilter.GetComponent<Renderer>();
        if (renderer.sharedMaterial == null)
            // Debug.Log("This");
            renderer.sharedMaterial = Resources.Load<Material>("RoadMaterial");
        // meshFilter.GetComponent<Renderer>().sharedMaterial;
        renderer.sharedMaterial.SetTexture("texture", texture);
            gameObject.layer = LayerMask.NameToLayer("Ground");
    }

    void OnDrawGizmos()
    {
        // GeneratePoints();
        // Handles.matrix = transform.localToWorldMatrix;

        // BuildMesh();
    }
}

