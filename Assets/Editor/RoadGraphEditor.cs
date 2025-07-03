using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(RoadGraph))]
public class RoadGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // shows the default inspector

        RoadGraph roadGraph = (RoadGraph)target;
        string loadPath = RoadGraphSerializer.loadPath;
        string savePath = RoadGraphSerializer.savePath;

        GUILayout.Space(10);
        if (GUILayout.Button("Load Graph from File"))
        {
            string jsonData = File.ReadAllText(loadPath);

            roadGraph.Nodes = RoadGraphSerializer.DeserializeGraph(jsonData);
            Debug.Log("Graph loaded into scene object.");
        }

        if (GUILayout.Button("Save Graph to File"))
        {
            RoadGraphSerializer.SaveGraph(roadGraph.Nodes, savePath);
            Debug.Log($"Graph saved from scene object to {savePath}.");
        }
        if (GUILayout.Button("Clear Nodes"))
        {
            roadGraph.Clear();
            Debug.Log("Cleared");
        }
        if (GUILayout.Button("Rebuild Graph"))
        {
            roadGraph.RebuildGraph();
            Debug.Log("Rebuilt");
        }
    }
}