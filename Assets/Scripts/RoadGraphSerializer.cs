using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
    private static string fileName
    {
        get
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName switch
            {
                "VR" => "graph_data.dat",
                "EasyMobile" => "graph_data_0.dat",
                _ => throw new NotImplementedException()
            };
        }
    }

    private static string persistentPath => Path.Combine(Application.persistentDataPath, fileName);
    private static string streamingPath => Path.Combine(Application.streamingAssetsPath, fileName);
    public static string loadPath => streamingPath;
    public static string savePath => loadPath;

    public static void SaveGraph(List<LaneNode> nodes, string savePath = "")
    {
        string targetPath = string.IsNullOrEmpty(savePath) ? persistentPath : savePath;
        RoadGraphData graphData = new RoadGraphData();
        var nodeIndexMap = new Dictionary<LaneNode, int>();

        for (int i = 0; i < nodes.Count; i++)
            nodeIndexMap[nodes[i]] = i;

        foreach (var node in nodes)
        {
            var serializable = new LaneNodeSerializable { position = node.Position };

            foreach (var outNode in node.Outgoing)
            {
                if (nodeIndexMap.TryGetValue(outNode, out int idx))
                {
                    serializable.outgoingIndices.Add(idx);
                }
            }
            graphData.nodes.Add(serializable);
        }

        string json = JsonUtility.ToJson(graphData, true);
        File.WriteAllText(targetPath, json);
        Debug.Log($"Graph saved to: {targetPath}, nodes: {graphData.nodes.Count}");
    }

    public static IEnumerator LoadGraphCoroutine(Action<List<LaneNode>> onComplete)
    {
        // if (!File.Exists(persistentPath))
        // {
#if UNITY_ANDROID
            UnityWebRequest www = UnityWebRequest.Get(streamingPath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(persistentPath, www.downloadHandler.text);
                Debug.Log("Graph copied from StreamingAssets to persistentDataPath.");
            }
            else
            {
                Debug.LogError("Failed to read graph from StreamingAssets: " + www.error);
                onComplete(new List<LaneNode>());
                yield break;
            }
#else
            if (File.Exists(streamingPath))
            {
                File.Copy(streamingPath, persistentPath);
                Debug.Log("Graph copied from StreamingAssets to persistentDataPath.");
            }
            else
            {
                Debug.LogError("Graph file missing in StreamingAssets.");
                onComplete(new List<LaneNode>());
                yield break;
            }
#endif
        // }

        string json = File.ReadAllText(persistentPath);
        onComplete(DeserializeGraph(json));
    }

    public static List<LaneNode> DeserializeGraph(string json)
    {
        var graphData = JsonUtility.FromJson<RoadGraphData>(json);
        var nodes = new List<LaneNode>();

        foreach (var nodeData in graphData.nodes)
            nodes.Add(new LaneNode { Position = nodeData.position });

        for (int i = 0; i < graphData.nodes.Count; i++)
        {
            foreach (int outIdx in graphData.nodes[i].outgoingIndices)
            {
                nodes[i].Outgoing.Add(nodes[outIdx]);
                nodes[outIdx].Incoming.Add(nodes[i]);
            }
        }

        return nodes;
    }
}
