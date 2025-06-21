// using UnityEngine;
// using UnityEditor;

// [CustomEditor(typeof(RoadFromGraphMod))]
// public class RoadGraphEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();

//         RoadFromGraphMod roadGraphMod = (RoadFromGraphMod)target;

//         GUILayout.Space(10);

//         if (GUILayout.Button("Save Graph to File"))
//         {
//             if (roadGraphMod.roadGraph != null)
//             {
//                 var path = System.IO.Path.Combine(Application.persistentDataPath, "graph_data.dat");
//                 RoadGraphSerializer.SaveGraph(roadGraphMod.roadGraph.Nodes, path);
//                 Debug.Log("Graph saved to: " + path);
//             }
//             else
//             {
//                 Debug.LogWarning("No RoadGraph assigned.");
//             }
//         }
//     }
// }
