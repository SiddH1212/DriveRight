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
        // public float step = 0.1f;
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
        // intersectionMaterial = (Material)Resources.Load("IntersectionMaterial");
        GUILayout.Space(8);
        EditorGUI.BeginDisabledGroup(endpoints.Count < 2 || intersectionMaterial == null);
        if (GUILayout.Button("Create Intersection", GUILayout.Height(30)))
            CreateIntersection();
        EditorGUI.EndDisabledGroup();
    }

    // public void Setup(List <RoadGenerator> rgList, List <int> knotList){
    //     for (int i = 0; i < rgList.Count; i++){
    //         endpoints.Add(new EndpointEntry());
    //         var entry = endpoints[i];
    //         entry.container = rgList[i].GetComponent<SplineContainer>();
    //         entry.knotChoice = knotList[i];
    //     }
    // }
    private void CreateIntersection()
    {
        
        var worldPointsTangents = new List<List <Vector3>>();
        var incomingLanePoints = new List<List<Vector3>>();
        var outgoingLanePoints = new List<List<Vector3>>();
        var lanePointTangents = new List<Vector3>();

        // Collect world points by computing local offsets then transforming properly
        foreach (var entry in endpoints)
        {
            if (entry.container == null) continue;
            float t = entry.knotChoice == 0 ? 0f : 1f;
            int n_lanes = entry.container.GetComponent<RoadGenerator>().n_lanes;
            entry.container.Evaluate(0, t, out float3 posF, out float3 tanF, out float3 upF);

            List <Vector3> incomingPts = new List<Vector3>();
            List <Vector3> outgoingPts = new List<Vector3>();

            for (int i = 0; i < n_lanes; i++){
                var pLanes = entry.container.GetComponent<RoadGenerator>().pLanes;
                Debug.Log("Num Lanes: "+pLanes.Count);
                Debug.Log(t);
                Vector3 incomingPt = pLanes[t == 0f ? 0 : pLanes.Count - 1][t == 0f ? 2*n_lanes-1-i : i];
                incomingPts.Add(incomingPt);
                Debug.Log(t == 0f ? i : 2 * n_lanes - 1 - i);
                Debug.Log(pLanes[0].Count());
                Vector3 outgoingPt = pLanes[t == 0f ? 0 : pLanes.Count - 1][t == 0f ? i : 2*n_lanes-1-i];
                outgoingPts.Add(outgoingPt);
            }
            incomingLanePoints.Add(incomingPts);
            outgoingLanePoints.Add(outgoingPts);
            lanePointTangents.Add(tanF);

            // Local position & directions
            Vector3 localPos = entry.container.transform.InverseTransformPoint((Vector3)posF);
            Vector3 localTangent = entry.container.transform.InverseTransformDirection((Vector3)tanF).normalized;
            Vector3 localUp = entry.container.transform.InverseTransformDirection((Vector3)upF).normalized;
            float halfWidth = entry.container.TryGetComponent<RoadGenerator>(out var rg) ? rg.width : 5f;
            Vector3 localRight = Vector3.Cross(localTangent, localUp).normalized;

            // Generate local in/out and convert back to world
            Vector3 outLocal = localPos + localRight * halfWidth;
            Vector3 inLocal  = localPos - localRight * halfWidth;
            worldPointsTangents.Add(new List<Vector3> (){entry.container.transform.TransformPoint(outLocal), entry.container.transform.TransformDirection(localTangent)});
            worldPointsTangents.Add(new List<Vector3> (){entry.container.transform.TransformPoint(inLocal), entry.container.transform.TransformDirection(localTangent)});
            // worldPoints.Add(entry.container.transform.TransformPoint(inLocal));
        }
        if (worldPointsTangents.Count < 4)
        {
            Debug.LogError("Need at least two endpoints (4 points) to form intersection.");
            return;
        }

        // Sort world points clockwise around center
        var center = Vector3.zero; worldPointsTangents.ForEach(p => center += p[0]); center /= worldPointsTangents.Count;
        var sorted = new List<List <Vector3>>(worldPointsTangents);
        sorted.Sort((a, b) => Mathf.Atan2(a[0].z - center.z, a[0].x - center.x)
                                 .CompareTo(Mathf.Atan2(b[0].z - center.z, b[0].x - center.x)));

        // Build mesh in local space of a neutral GameObject
        int idx = 0;
        string name = "SplineIntersection_";
        while (GameObject.Find(name+idx) != null) idx++;
        var meshGO = new GameObject(name+idx);
        meshGO.transform.position = center;
        meshGO.transform.rotation = Quaternion.identity;
        meshGO.transform.localScale = Vector3.one;
        var mf = meshGO.AddComponent<MeshFilter>();
        var mr = meshGO.AddComponent<MeshRenderer>();
        var mc = meshGO.AddComponent<MeshCollider>();
        mr.sharedMaterial = intersectionMaterial;

        // Convert sorted to local
        var vertsLocal = new List <Vector3> () {meshGO.transform.InverseTransformPoint(center)};
        for (int i = 0; i < sorted.Count; i++)
        {
            var p1 = sorted[i];
            var p2 = sorted[(i+1)%sorted.Count];

            if (sorted[i][1] == sorted[(i+1)%sorted.Count][1]){
                vertsLocal.Add(meshGO.transform.InverseTransformPoint(p1[0]));
                continue;
            }

            Vector3 intersectionPoint = CalculateIntersection(p1, p2); //calculate this intersection point
            for (float j = 0; j < 1; j+=step){
                Vector3 pa = Vector3.Lerp (p1[0], intersectionPoint, j);
                Vector3 pb = Vector3.Lerp (intersectionPoint, p2[0], j);
                Vector3 pc = Vector3.Lerp (pa, pb, j);
                vertsLocal.Add(meshGO.transform.InverseTransformPoint(pc));
            }

        }
        // Debug.Log(vertsLocal.Count);
        var mesh = new Mesh();
        // mesh.vertices = new Vector3 (); 
        var vertices = new List<Vector3>();
        for (int i = 0; i < vertsLocal.Count; i++) vertices.Add(vertsLocal[i]);
        mesh.vertices = vertices.ToArray();
        var tris = new List<int>(); 
        for (int i = 1; i < vertsLocal.Count; i++) tris.AddRange(new[] { 0, 1 + i%(vertsLocal.Count-1), 1 + (i-1)%(vertsLocal.Count-1)});
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mc.sharedMaterial = Resources.Load<PhysicMaterial>("Materials/Road");

        // add debugger
        // var dbg = meshGO.AddComponent<IntersectionDebugger>(); dbg.originalPoints = worldPoints; dbg.sortedPoints = sorted;
        Selection.activeGameObject = meshGO;
        meshGO.layer = LayerMask.NameToLayer("Ground");

        // whenever this intersection is created, connect the unconnected lane centerpoint Node at the end of road-1, to the unconnected lanepoint centerpoint at the start of road-2 and road-3 for the corresponding lane, depicting the reasonable paths to be followed by a vehicle from that incoming unconnected end node on road-1
        var graph = FindObjectOfType<RoadGraph>();
        Debug.Log(graph.Nodes.Count + "is number of nodes before creation");
        if (graph == null)
        {
            Debug.LogWarning("Graph not found — skipping graph connections.");
            return;
        }

        int laneCount = worldPointsTangents[0].Count; // Assuming bidirectional lanes per road

        // Create center nodes if needed
        LaneNode GetOrCreateNode(Vector3 pos, float maxDistance = 2f)
        {
            // pos = meshGO.transform.InverseTransformPoint(pos);
            var node = graph.GetClosestNode(pos, maxDistance);
            if (node == null)
            {
                node = new LaneNode {Position = pos};
                graph.Nodes.Add(node);
            }
            return node;
        }

        for (int i = 0; i < incomingLanePoints.Count; i++){
            for (int j = 0; j < outgoingLanePoints.Count; j++){
                if (i == j) continue;
                for (int k = 0; k < incomingLanePoints[i].Count; k++){
                    // var fromNode = GetOrCreateNode(incomingLanePoints[i][k]);
                    // var toNode = GetOrCreateNode(outgoingLanePoints[j][k]);
                    // if (!fromNode.Outgoing.Contains(toNode))
                    //     fromNode.Outgoing.Add(toNode);
                    Vector3 p1 = incomingLanePoints[i][k];
                    Vector3 p2 = outgoingLanePoints[j][k];
                    Vector3 pIntersection = CalculateIntersection(new List<Vector3> {incomingLanePoints[i][k], lanePointTangents[i]}, new List<Vector3> {outgoingLanePoints[j][k], lanePointTangents[j]});
                    var fromNode = GetOrCreateNode(p1, 0.01f);
                    for (float t = 0.1f; t <= 1f; t+=0.1f){
                        Vector3 pa = Vector3.Lerp (p1, pIntersection, t);
                        Vector3 pb = Vector3.Lerp (pIntersection, p2, t);
                        Vector3 pc = Vector3.Lerp (pa, pb, t);
                        var toNode = GetOrCreateNode(pc, 0.01f);
                        if (!fromNode.Outgoing.Contains(toNode)) fromNode.Outgoing.Add(toNode);
                        fromNode = toNode;
                        t = (float)Math.Round(t, 2);
                    }
                }
            }
        }

        // var containers = endpoints.Select(e => e.container).ToList();
        // var knotChoices = endpoints.Select(e => e.knotChoice).ToList();
        // center = meshGO.transform.position;

        // FindObjectOfType<IntersectionManager>().SaveIntersection(containers, knotChoices, meshGO.name, center);

    }

    private Vector3 CalculateIntersection(List<Vector3> e1, List<Vector3> e2)
    {
        // Extract positions and directions
        Vector3 p1 = e1[0];
        Vector3 d1 = e1[1].normalized;
        Vector3 p2 = e2[0];
        Vector3 d2 = e2[1].normalized;

        // Work in XZ plane
        Vector2 P1 = new Vector2(p1.x, p1.z);
        Vector2 D1 = new Vector2(d1.x, d1.z);
        Vector2 P2 = new Vector2(p2.x, p2.z);
        Vector2 D2 = new Vector2(d2.x, d2.z);

        // Solve P1 + t D1 = P2 + u D2  =>  t D1 - u D2 = (P2 - P1)
        float det = D1.x * -D2.y - D1.y * -D2.x;
        if (Mathf.Abs(det) < 1e-5f)
        {
            // If nearly parallel: fall back to midpoint
            Vector3 mid = (p1 + p2) * 0.5f;
            return new Vector3(mid.x, (p1.y + p2.y) * 0.5f, mid.z);
        }

        Vector2 rhs = P2 - P1;
        // Cramers rule for t:
        float t = ( rhs.x * -D2.y - rhs.y * -D2.x ) / det;

        Vector2 inter2D = P1 + D1 * t;
        // Recover world‐space Y by linear interp on the corner Ys
        float y = Mathf.Lerp(p1.y, p2.y, t / (t + ((rhs - D1 * t).magnitude / D2.magnitude)));

        return new Vector3(inter2D.x, y, inter2D.y);
    }
}


// Intersection Debugging
// [ExecuteAlways]
public class IntersectionDebugger : MonoBehaviour
{
    public List<Vector3> originalPoints = new List<Vector3>();
    public List<Vector3> sortedPoints = new List<Vector3>();
    void OnDrawGizmos()
    {
        if (originalPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < originalPoints.Count; i++)
            {
                Gizmos.DrawSphere(originalPoints[i], 0.1f);
#if UNITY_EDITOR
                Handles.Label(originalPoints[i], $"O{i}");
#endif
            }
        }
        if (sortedPoints != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                Gizmos.DrawSphere(sortedPoints[i], 0.15f);
#if UNITY_EDITOR
                Handles.Label(sortedPoints[i], $"S{i}");
#endif
            }
        }
    }
}