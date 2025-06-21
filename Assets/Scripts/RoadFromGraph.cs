// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Splines;
// using UnityEditor;
// // using Unity.PlasticSCM.Editor.WebApi;
// using Unity.VisualScripting;
// using System.Collections;

// public class RoadFromGraph : MonoBehaviour
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
//         // 1) Load the graph
//         // var nodes = RoadGraphSerializer.LoadGraph(graphJsonFile);
//         // // 2) Re‐create the .Outgoing references
//         // RoadGraphSerializer.RebuildOutgoing(nodes);
//         // 3) Generate scene geometry
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

//     // void ConstructNetwork(LaneNode laneNode)
//     // {
//     //     if (!visited.ContainsKey(laneNode)) visited[laneNode] = new HashSet<LaneNode>();
//     //     foreach (var outgoingNode in laneNode.Outgoing)
//     //     {
//     //         var path = new List<Vector3> { laneNode.Position };
//     //         Material mat = null;
//     //         path.Add(outgoingNode.Position);
//     //         if (visited[laneNode].Contains(outgoingNode)) return;
//     //         if (laneNode.Outgoing.Count > 1 || outgoingNode.Incoming.Count > 1) mat = intersectionMaterial;
//     //         CreateSplineRoad(path, mat);
//     //         visited[laneNode].Add(outgoingNode);
//     //         if (visited.ContainsKey(outgoingNode)) visited[outgoingNode].Add(laneNode);
//     //         else visited[outgoingNode] = new HashSet<LaneNode>();
//     //         ConstructNetwork(outgoingNode);
//     //     }
//     //     done.Add(laneNode);
//     // }

//     // void ConstructNetwork(LaneNode node)
//     // {
//     //     if (node.Outgoing.Count == 1){}
//     // }

//     bool IsIntersectionEdge(LaneNode from, LaneNode to)
//     {
//         if (from.Outgoing.Count > 1 || to.Incoming.Count > 1) return true;
//         else return false;
//     }

//     void GenerateFromGraph(List<LaneNode> nodes)
//     {
//         visited = new Dictionary<LaneNode, HashSet<LaneNode>>();

//         foreach (var node in nodes)
//         {
//             if (done.Contains(node)) continue;
//             var current = node;
//             // while (current.Incoming.Count == 1 && !done.Contains(current.Incoming[0]) && !IsIntersectionEdge(current.Incoming[0], node) && current.Incoming[0] != node)
//             // {
//             //     current = node.Incoming[0];
//             // }
//             // ConstructNetwork(node);
//             var path = new List<Vector3> { current.Position };
//             var tangents = new List<Vector3>();
//             if (current.Incoming.Count == 1 && !IsIntersectionEdge(current.Incoming[0], current)) tangents.Add((current.Position - current.Incoming[0].Position).normalized);
//             else if (current.Outgoing.Count == 1) tangents.Add((current.Outgoing[0].Position - current.Position).normalized);
//             else
//             {
//                 Vector3 buff = Vector3.zero;
//                 foreach (var outgoer in current.Outgoing) buff += outgoer.Position - current.Position;
//                 buff.Normalize();
//                 tangents.Add(buff.normalized);
//             }

//             while (current.Outgoing.Count == 1 && current.Outgoing[0].Incoming.Count == 1)
//             {
//                 done.Add(current);
//                 path.Add(current.Outgoing[0].Position);
//                 tangents.Add((current.Outgoing[0].Position - current.Position).normalized);
//                 current = current.Outgoing[0];
//             }
//             if (path.Count > 1) CreateSplineRoad(path, defaultRoadMaterial, tangents);
//             var prev_tangent = tangents[tangents.Count - 1];

//             foreach (var outgoer in current.Outgoing)   // now the outgoers left are definitely intersections, as non intersection outgoers on this road are done
//             {
//                 path = new List<Vector3> { current.Position, outgoer.Position };
//                 tangents = new List<Vector3> { prev_tangent, Vector3.zero };

//                 float minAngle = 181;
//                 Vector3 minAnglePos = current.Position;
//                 if (current.Incoming.Count == 1 && !IsIntersectionEdge(current.Incoming[0], current)) prev_tangent = (current.Position - current.Incoming[0].Position).normalized;
//                 else // if (current.Incoming.Count > 1)
//                 {
//                     minAnglePos = 2*current.Position-outgoer.Position;
//                     // foreach (var incomer in current.Incoming)
//                     // {
//                     //     if (!IsIntersectionEdge(current, outgoer)) { minAnglePos = incomer.Position;  }
//                     //     // var angleFromPrev = Vector3.Angle(outgoer.Position - current.Position, current.Position - incomer.Position);
//                     //     // if (angleFromPrev < minAngle)
//                     //     // {
//                     //     //     minAngle = angleFromPrev;
//                     //     //     minAnglePos = incomer.Position;
//                     //     // }
//                     //     // // tangents[1] += second.Position - outgoer.Position;
//                     // }
//                     tangents[0] = (current.Position - minAnglePos).normalized;

//                 }


//                 minAngle = 181;
//                 minAnglePos = outgoer.Position;
//                 if (outgoer.Outgoing.Count == 0) Debug.Log("No outgoing at: " + outgoer.Position);
//                 foreach (var second in outgoer.Outgoing)
//                 {
//                     var angleFromPrev = Vector3.Angle(second.Position - outgoer.Position, tangents[0]);
//                     Debug.Log("Angle" + angleFromPrev);
//                     if (angleFromPrev < minAngle)
//                     {
//                         minAngle = angleFromPrev;
//                         minAnglePos = second.Position;
//                     }
//                     // tangents[1] += second.Position - outgoer.Position;
//                 }
//                 if (minAnglePos == outgoer.Position) Debug.Log("No success at: " + outgoer.Position );
//                 tangents[1] = (minAnglePos - outgoer.Position).normalized;
//                 // tangents[1] = tangents[1].normalized;

//                 CreateSplineRoad(path, intersectionMaterial, tangents);

//                 // done.Add(outgoer);
//                 // buff.Normalize();
//                 // tangents.Add(buff);


//             }
//             done.Add(current);



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

//     void CreateSplineRoad(List<Vector3> worldPath, Material defaultRoadMaterial = null, List<Vector3> tangents = null)
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
//         for (int i = 0; i < worldPath.Count; i++)
//         {
//             var wp = worldPath[i];
//             // var local = go.transform.InverseTransformPoint(wp);
//             var local = go.transform.InverseTransformPoint(wp);
//             var tangentLocal = go.transform.InverseTransformDirection(tangents[i]).normalized;
//             int mag = 3;
//             spline.Add(new BezierKnot(local, -mag * Vector3.forward, mag*Vector3.forward, Quaternion.Euler(0, Mathf.Rad2Deg * Mathf.Atan2(tangentLocal.x, tangentLocal.z), 0)));
//         }
//         // spline.SetTangentMode(TangentMode.AutoSmooth);

//         // RoadGenerator
//         var rg = go.AddComponent<RoadGenerator>();
//         rg.splineContainer = sc;
//         rg.meshFilter = meshFilter;

//         rg.width = 3.6f;
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
