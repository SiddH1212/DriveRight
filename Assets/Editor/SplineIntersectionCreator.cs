
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using Unity.Mathematics;
using System;
using System.Linq;

public class SplineIntersectionCreator : EditorWindow
{
    [System.Serializable]
    public class EndpointEntry
    {
        public SplineContainer container;
        public int knotChoice = 0; // 0 = start, 1 = end
    }
    public float step = 0.1f;
    [SerializeField]
    private List<EndpointEntry> endpoints = new List<EndpointEntry>();
    private Material intersectionMaterial;

    [MenuItem("Tools/Spline Intersection Creator")]
    public static void ShowWindow() => GetWindow<SplineIntersectionCreator>("Spline Intersection Creator");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Configure endpoints and intersection material, then press Create.", MessageType.Info);
        GUILayout.Space(4);

        if (GUILayout.Button("Add Endpoint", GUILayout.Height(25)))
            endpoints.Add(new EndpointEntry());

        EditorGUILayout.LabelField($"Endpoints: {endpoints.Count}", EditorStyles.boldLabel);
        for (int i = 0; i < endpoints.Count; i++)
        {
            var entry = endpoints[i];
            EditorGUILayout.BeginHorizontal();
            entry.container = (SplineContainer)EditorGUILayout.ObjectField(entry.container, typeof(SplineContainer), true, GUILayout.Width(200));
            entry.knotChoice = EditorGUILayout.Popup(entry.knotChoice, new[] { "Start", "End" }, GUILayout.Width(60));
            if (GUILayout.Button("Remove", GUILayout.Width(60))) { endpoints.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
            endpoints[i] = entry;
        }

        GUILayout.Space(8);
        intersectionMaterial = (Material)EditorGUILayout.ObjectField("Intersection Material:", intersectionMaterial, typeof(Material), false);
        GUILayout.Space(8);
        EditorGUI.BeginDisabledGroup(endpoints.Count < 2 || intersectionMaterial == null);
        if (GUILayout.Button("Create Intersection", GUILayout.Height(30)))
            CreateIntersection();
        EditorGUI.EndDisabledGroup();
    }

    private void CreateIntersection()
    {
        var worldPointsTangents = new List<List<Vector3>>();
        var incomingLanePoints = new List<List<Vector3>>();
        var outgoingLanePoints = new List<List<Vector3>>();
        var lanePointTangents = new List<Vector3>();

        // Collect world points by computing local offsets then transforming properly
        foreach (var entry in endpoints)
        {
            if (entry.container == null) continue;
            float t = entry.knotChoice == 0 ? 0f : 1f;
            var rg = entry.container.GetComponent<RoadGenerator>();
            bool bidirectional = rg.bidirectional;
            int n_lanes = rg.n_lanes;
            entry.container.Evaluate(0, t, out float3 posF, out float3 tanF, out float3 upF);

            List<Vector3> incomingPts = new List<Vector3>();
            List<Vector3> outgoingPts = new List<Vector3>();

            for (int i = 0; i < n_lanes; i++)
            {
                var pLanes = entry.container.GetComponent<RoadGenerator>().pLanes;
                Vector3 incomingPt = pLanes[t == 0f ? 0 : pLanes.Count - 1][t == 0f ? (bidirectional ? 2 : 1) * n_lanes - 1 - i : i];
                incomingPts.Add(incomingPt);
                if (!bidirectional) continue;
                Vector3 outgoingPt = pLanes[t == 0f ? 0 : pLanes.Count - 1][t == 0f ? i : (bidirectional ? 2 : 1) * n_lanes - 1 - i];
                outgoingPts.Add(outgoingPt);
            }

            incomingLanePoints.Add(incomingPts);
            outgoingLanePoints.Add(outgoingPts);
            if (t != 0f) tanF = -tanF;
            lanePointTangents.Add(tanF);

            // Local position & directions
            Vector3 localPos = entry.container.transform.InverseTransformPoint((Vector3)posF);
            Vector3 localTangent = entry.container.transform.InverseTransformDirection((Vector3)tanF).normalized;
            Vector3 localUp = entry.container.transform.InverseTransformDirection((Vector3)upF).normalized;
            float halfWidth = entry.container.TryGetComponent<RoadGenerator>(out var rog) ? rg.width : 5f;
            Vector3 localRight = Vector3.Cross(localTangent, localUp).normalized;

            // Generate local in/out and convert back to world
            Vector3 outLocal = localPos + localRight * halfWidth;
            Vector3 inLocal = localPos - localRight * halfWidth;
            worldPointsTangents.Add(new List<Vector3>() { entry.container.transform.TransformPoint(outLocal), entry.container.transform.TransformDirection(localTangent) });
            worldPointsTangents.Add(new List<Vector3>() { entry.container.transform.TransformPoint(inLocal), entry.container.transform.TransformDirection(localTangent) });
        }

        if (worldPointsTangents.Count < 4)
        {
            Debug.LogError("Need at least two endpoints (4 points) to form intersection.");
            return;
        }

        // Sort world points clockwise around center
        var center = Vector3.zero; worldPointsTangents.ForEach(p => center += p[0]); center /= worldPointsTangents.Count;
        var sorted = new List<List<Vector3>>(worldPointsTangents);
        sorted.Sort((a, b) => Mathf.Atan2(a[0].z - center.z, a[0].x - center.x)
                                 .CompareTo(Mathf.Atan2(b[0].z - center.z, b[0].x - center.x)));

        // Build mesh in local space of a neutral GameObject
        int idx = 0;
        string name = "SplineIntersection_";
        while (GameObject.Find(name + idx) != null) idx++;
        var meshGO = new GameObject(name + idx);
        meshGO.transform.position = center;
        meshGO.transform.rotation = Quaternion.identity;
        meshGO.transform.localScale = Vector3.one;
        var mf = meshGO.AddComponent<MeshFilter>();
        var mr = meshGO.AddComponent<MeshRenderer>();
        var mc = meshGO.AddComponent<MeshCollider>();
        mr.sharedMaterial = intersectionMaterial;

        // Convert sorted to local
        var vertsLocal = new List<Vector3>() { meshGO.transform.InverseTransformPoint(center) };
        for (int i = 0; i < sorted.Count; i++)
        {
            var p1 = sorted[i];
            var p2 = sorted[(i + 1) % sorted.Count];

            if (sorted[i][1] == sorted[(i + 1) % sorted.Count][1])
            {
                vertsLocal.Add(meshGO.transform.InverseTransformPoint(p1[0]));
                continue;
            }

            float ctrlRadius = Vector3.Distance(p1[0], p2[0]) / 4f;
            Vector3 ctrlPoint1 = p1[0] - p1[1] * ctrlRadius;
            Vector3 ctrlPoint2 = p2[0] - p2[1] * ctrlRadius;
            for (float j = 0; j < 1; j += step)
            {
                Vector3 pa = Vector3.Lerp(p1[0], ctrlPoint1, j);
                Vector3 pb = Vector3.Lerp(ctrlPoint2, p2[0], j);
                Vector3 pc = Vector3.Lerp(ctrlPoint1, ctrlPoint2, j);
                Vector3 pd = Vector3.Lerp(pa, pc, j);
                Vector3 pe = Vector3.Lerp(pc, pb, j);
                Vector3 pf = Vector3.Lerp(pd, pe, j);
                vertsLocal.Add(meshGO.transform.InverseTransformPoint(pf));
            }
        }

        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        for (int i = 0; i < vertsLocal.Count; i++) vertices.Add(vertsLocal[i]);
        mesh.vertices = vertices.ToArray();
        var tris = new List<int>();
        for (int i = 1; i < vertsLocal.Count; i++) tris.AddRange(new[] { 0, 1 + i % (vertsLocal.Count - 1), 1 + (i - 1) % (vertsLocal.Count - 1) });
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mc.sharedMaterial = Resources.Load<PhysicMaterial>("Materials/Road");

        Selection.activeGameObject = meshGO;
        meshGO.layer = LayerMask.NameToLayer("Ground");

        // --- Debugger: attach IntersectionDebugger and fill point lists for scene gizmos ---
        var dbg = meshGO.AddComponent<IntersectionDebugger>();

        // originalPoints: the raw world points we computed (before sorting)
        dbg.originalPoints = worldPointsTangents.Select(wp => wp[0]).ToList();

        // sortedPoints: the points after clockwise sorting
        dbg.sortedPoints = sorted.Select(wp => wp[0]).ToList();

        // edge end points: gather unique incoming/outgoing lane endpoints used for graph connections
        var edgeEnds = new List<Vector3>();
        void AddUniquePoint(Vector3 p)
        {
            if (!edgeEnds.Any(e => Vector3.Distance(e, p) < 0.01f)) edgeEnds.Add(p);
        }

        foreach (var list in incomingLanePoints) foreach (var p in list) AddUniquePoint(p);
        foreach (var list in outgoingLanePoints) foreach (var p in list) AddUniquePoint(p);
        dbg.edgeEndPoints = edgeEnds;

        // show labels by default in the debugger
        dbg.showLabels = true;

        // whenever this intersection is created, connect the unconnected lane centerpoint Node at the end of road-1, to the unconnected lanepoint centerpoint at the start of road-2 and road-3 for the corresponding lane, depicting the reasonable paths to be followed by a vehicle from that incoming unconnected end node on road-1
        var graph = FindObjectOfType<RoadGraph>();
        Debug.Log(graph.Nodes.Count + "is number of nodes before creation");
        if (graph == null)
        {
            Debug.LogWarning("Graph not found — skipping graph connections.");
            return;
        }

        // Create center nodes if needed
        LaneNode GetOrCreateNode(Vector3 pos, float maxDistance = 2f)
        {
            var node = graph.GetClosestNode(pos, maxDistance);
            if (node == null)
            {
                node = new LaneNode { Position = pos };
                graph.Nodes.Add(node);
            }
            return node;
        }

        for (int i = 0; i < incomingLanePoints.Count; i++)
        {
            for (int j = 0; j < outgoingLanePoints.Count; j++)
            {
                int n_incoming = incomingLanePoints[i].Count;
                int n_outgoing = outgoingLanePoints[j].Count;
                if (i == j || n_incoming == 0 || n_outgoing == 0) continue;
                for (int k = 0; k < n_incoming; k++)
                {
                    Vector3 p1 = incomingLanePoints[i][k];
                    Vector3 p2 = outgoingLanePoints[j][Math.Min(k, n_outgoing - 1)];

                    float ctrlRadius = Vector3.Distance(p1, p2) / 2.5f;
                    Vector3 ctrlPoint1 = p1 - lanePointTangents[i].normalized * ctrlRadius;
                    Vector3 ctrlPoint2 = p2 - lanePointTangents[j].normalized * ctrlRadius;

                    var fromNode = GetOrCreateNode(p1, 0.01f);
                    for (float t = 0.1f; t <= 1f; t += 0.1f)
                    {
                        Vector3 pa = Vector3.Lerp(p1, ctrlPoint1, t);
                        Vector3 pb = Vector3.Lerp(ctrlPoint2, p2, t);
                        Vector3 pc = Vector3.Lerp(ctrlPoint1, ctrlPoint2, t);
                        Vector3 pd = Vector3.Lerp(pa, pc, t);
                        Vector3 pe = Vector3.Lerp(pc, pb, t);
                        Vector3 pf = Vector3.Lerp(pd, pe, t);
                        var toNode = GetOrCreateNode(pf, 0.01f);
                        if (!fromNode.Outgoing.Contains(toNode)) fromNode.Outgoing.Add(toNode);
                        fromNode = toNode;
                        t = (float)Math.Round(t, 2);
                    }
                }
            }
        }
    }
}

// Intersection Debugging
[ExecuteAlways]
public class IntersectionDebugger : MonoBehaviour
{
    public List<Vector3> originalPoints = new List<Vector3>();
    public List<Vector3> sortedPoints = new List<Vector3>();
    public List<Vector3> edgeEndPoints = new List<Vector3>();
    public bool showLabels = false;

    // Radii for the spheres are public so you can tweak in inspector if needed
    public float originalRadius = 0.08f;
    public float sortedRadius = 0.12f;
    public float edgeRadius = 0.14f;

    void OnDrawGizmos()
    {
        if (originalPoints != null && originalPoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < originalPoints.Count; i++)
            {
                Gizmos.DrawSphere(originalPoints[i], originalRadius);
#if UNITY_EDITOR
                if (showLabels) Handles.Label(originalPoints[i], $"O{i}");
#endif
            }
        }
        if (sortedPoints != null && sortedPoints.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                Gizmos.DrawSphere(sortedPoints[i], sortedRadius);
#if UNITY_EDITOR
                if (showLabels) Handles.Label(sortedPoints[i], $"S{i}");
#endif
            }
        }
        if (edgeEndPoints != null && edgeEndPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < edgeEndPoints.Count; i++)
            {
                Gizmos.DrawSphere(edgeEndPoints[i], edgeRadius);
#if UNITY_EDITOR
                if (showLabels) Handles.Label(edgeEndPoints[i], $"E{i}");
#endif
            }
        }
    }
}

