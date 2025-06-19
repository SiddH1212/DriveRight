using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[Serializable]
public class LaneNodeSerializable
{
    public Vector3 position;
    public List<int> outgoingIndices = new List<int>();
}

[Serializable]
public class RoadGraphData
{
    public List<LaneNodeSerializable> nodes = new List<LaneNodeSerializable>();
}

public static class RoadGraphSerializer
{
    private static string defaultFilePath = Application.streamingAssetsPath + "/graph_data.dat";
    private static string filePath = defaultFilePath;
    public static void SaveGraph(List<LaneNode> nodes, string savePath)
    {
        int count = 0;
        if (savePath != "") filePath = savePath;
        else filePath = defaultFilePath;
        RoadGraphData graphData = new RoadGraphData();
        var nodeIndexMap = new Dictionary<LaneNode, int>();
        for (int i = 0; i < nodes.Count; i++)
            nodeIndexMap[nodes[i]] = i;
        Debug.Log(nodeIndexMap.Count);
        foreach (var node in nodes)
        {
            var serializable = new LaneNodeSerializable
            {
                position = node.Position
            };

            foreach (var outNode in node.Outgoing)
            {
                int idx;
                if (nodeIndexMap.TryGetValue(outNode, out idx))
                {
                    serializable.outgoingIndices.Add(idx);
                    count++;
                }
                else{
                    // Debug.Log("Unsuccessful");
                }
            }

            graphData.nodes.Add(serializable);
        }

        Debug.Log($"{graphData.nodes.Count} nodes present, {count} saving count outgoers");
        string json = JsonUtility.ToJson(graphData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Graph saved to: " + filePath);
    }

    public static List<LaneNode> LoadGraph(string loadPath = "")
    {
        if (loadPath != "") filePath = loadPath;
        else filePath = defaultFilePath;

        int count = 0;
        Debug.Log($"streaming path: {Application.streamingAssetsPath}");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Graph file not found at: " + filePath);
            return new List<LaneNode>();
        }

        string json = File.ReadAllText(filePath);
        var graphData = JsonUtility.FromJson<RoadGraphData>(json);
        var nodes = new List<LaneNode>();

        foreach (var nodeData in graphData.nodes)
        {
            nodes.Add(new LaneNode { Position = nodeData.position });
        }

        for (int i = 0; i < graphData.nodes.Count; i++)
        {
            count+=graphData.nodes[i].outgoingIndices.Count;
            foreach (int outIdx in graphData.nodes[i].outgoingIndices)
            {
                nodes[i].Outgoing.Add(nodes[outIdx]);
                nodes[outIdx].Incoming.Add(nodes[i]);
            }
        }
        Debug.Log(nodes.Count + " = num nodes loaded");
        Debug.Log(count + " = count of outgoers");
        Debug.Log("Graph loaded from: " + filePath);

        return nodes;
    }
}
