using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadGraph))]
public class RoadGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // shows the default inspector

        RoadGraph roadGraph = (RoadGraph)target;
        string loadPath = roadGraph.loadPath;
        string savePath = roadGraph.savePath;

        GUILayout.Space(10);
        if (GUILayout.Button("Load Graph from File"))
        {

            roadGraph.Nodes = RoadGraphSerializer.LoadGraph(loadPath);
            Debug.Log("Graph loaded into scene object.");
        }

        if (GUILayout.Button("Save Graph to File"))
        {
            RoadGraphSerializer.SaveGraph(roadGraph.Nodes, savePath);
            Debug.Log("Graph saved from scene object.");
        }
    }
}
