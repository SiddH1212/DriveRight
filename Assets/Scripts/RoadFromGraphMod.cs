// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Splines;
// using UnityEditor;
// using System.Collections;

// public class RoadFromGraphMod : MonoBehaviour
// {
//     // [Header("Graph Data")]
//     // public string graphJsonFile = "graph_data.dat";    // in StreamingAssets
//     public RoadGraph roadGraph;
//     public Material defaultRoadMaterial;
//     public Material intersectionMaterial;
//     HashSet<LaneNode> done = new HashSet<LaneNode>();
//     Dictionary <LaneNode, HashSet<LaneNode>> visited;

//     void Start()
//     {
//         intersectionMaterial = (Material)Resources.Load("IntersectionMaterial");
//         roadGraph = FindObjectOfType<RoadGraph>();
//         StartCoroutine(LoadAndGenerate());
//     }

//     IEnumerator LoadAndGenerate()
//     {
//         yield return StartCoroutine(roadGraph.LoadGraphCoroutine());
//         if (roadGraph.Nodes != null && roadGraph.Nodes.Count > 0)
//         {
//             GenerateFromGraph(roadGraph.Nodes);
//         }
//         else
//         {
//             Debug.LogError("Failed to generate roads: graph is empty or failed to load.");
//         }
//     }

//     void ConstructNetwork(LaneNode laneNode)
//     {
//         if (!visited.ContainsKey(laneNode)) visited[laneNode] = new HashSet<LaneNode>();
//         foreach (var outgoingNode in laneNode.Outgoing)
//         {
//             var path = new List<Vector3> { laneNode.Position };
//             if (visited[laneNode].Contains(outgoingNode)) return;
//             path.Add(outgoingNode.Position);
//             visited[laneNode].Add(outgoingNode);
//             if (visited.ContainsKey(outgoingNode)) visited[outgoingNode].Add(laneNode);
//             else visited[outgoingNode] = new HashSet<LaneNode>();
//             Material mat = null;
//             Debug.Log(outgoingNode.Incoming.Count);
//             if (laneNode.Outgoing.Count > 1 || outgoingNode.Incoming.Count > 1) mat = intersectionMaterial;
//             CreateSplineRoad(path, mat);
//             ConstructNetwork(outgoingNode);
//         }
//         done.Add(laneNode);
//     }

//     void GenerateFromGraph(List<LaneNode> nodes)
//     {
//         visited = new Dictionary<LaneNode, HashSet<LaneNode>>();

//         foreach (var node in nodes)
//         {
//             if (done.Contains(node)) continue;

//             ConstructNetwork(node);

//             // var path = new List<Vector3> { node.Position };
//             // visited.Add(node);


//             // // Straight segment if exactly one outgoing
//             // if (node.Outgoing.Count == 1)
//             // {
//             //     var path = new List<Vector3> { node.Position };
//             //     visited.Add(node);

//             //     var current = node;
//             //     while (true) //current.Outgoing.Count == 1)
//             //     {
//             //         current = current.Outgoing[0];
//             //         if (visited.Contains(current)) break;
//             //         path.Add(current.Position);
//             //         visited.Add(current);
//             //     }

//             //     if (path.Count >= 2)
//             //         CreateSplineRoad(path);
//             // }
//             // else if (node.Outgoing.Count > 1)
//             // {
//             //     // Intersection
//             //     CreateIntersectionMarker(node.Position);
//             //     visited.Add(node);
//             // }
//         }
//     }

//     void CreateSplineRoad(List<Vector3> worldPath, Material defaultRoadMaterial = null)
//     {
//         // GameObject + SplineContainer
//         var go = new GameObject("RoadSegment");
//         var sc = go.AddComponent<SplineContainer>();
//         var meshFilter = go.AddComponent<MeshFilter>();
//         var meshRenderer = go.AddComponent<MeshRenderer>();
//         var collider = go.AddComponent<MeshCollider>();

//         // Create a new (empty) spline
//         var serialized = new SerializedObject(sc);
//         var splinesProp = serialized.FindProperty("m_Splines");
//         var newIndex = splinesProp.arraySize-1;
//         // splinesProp.InsertArrayElementAtIndex(newIndex);
//         serialized.ApplyModifiedProperties();

//         // Now get that spline back
//         var spline = sc.Splines[newIndex];
//         spline.Clear(); // start empty

//         go.transform.position = go.transform.InverseTransformPoint(worldPath[0]);
//         // Add knots
//         foreach (var wp in worldPath)
//         {
//             // var local = go.transform.InverseTransformPoint(wp);
//             var local = go.transform.InverseTransformPoint(wp); 
//             spline.Add(new BezierKnot(local));
//         }
//         spline.SetTangentMode(TangentMode.AutoSmooth);

//         // RoadGenerator
//         var rg = go.AddComponent<RoadGenerator>();
//         rg.splineContainer = sc;
//         rg.meshFilter = meshFilter;

//         rg.width = 2.8f;
//         rg.step = 0.2f;

//         if (defaultRoadMaterial != null)
//             rg.GetComponent<Renderer>().sharedMaterial = defaultRoadMaterial;
            
//     }

//     void CreateIntersectionMarker(Vector3 worldPos)
//     {
//         var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//         go.name = "Intersection";
//         go.transform.position = worldPos;
//         go.transform.localScale = Vector3.one * 2f;
//         DestroyImmediate(go.GetComponent<Collider>());
//     }
// }
