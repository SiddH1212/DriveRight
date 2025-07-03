using System;
using System.Collections;
using System.Collections.Generic;
// using UnityEditor.Experimental.GraphView;
using UnityEngine;
// using UnityEngine.Splines;

[Serializable]
public class LaneNode
{
    public Vector3 Position { get; set; }
    public List<LaneNode> Outgoing = new List<LaneNode>();
    public List<LaneNode> Incoming = new List<LaneNode>();
}

public class RoadGraph : MonoBehaviour
{
    // All nodes across all roads
    // public string loadPath = Application.streamingAssetsPath + "/graph_data.dat";
    // public string loadPath = "/Users/mehulmathur/Desktop/Main Folder 0/Python Files/srfp/data_files/json_data/graph_data_isb_pullela_0.dat"; // IIITH_arnd.dat";
    // public string loadPath = "/Users/mehulmathur/Desktop/Main Folder 0/Python Files/srfp/data_files/json_data/graph_data_08.dat"; //Application.streamingAssetsPath + "/graph_data.dat";
    // public string savePath = Application.streamingAssetsPath + "/graph_data.dat";
    [HideInInspector] public List<LaneNode> Nodes = new List<LaneNode>();

    void Start()
    {
        // loadPath = "/Users/mehulmathur/Desktop/Main Folder 0/Python Files/srfp/data_files/json_data/graph_data_08.dat";
        // loadPath = Application.streamingAssetsPath + "/graph_data.dat";
        // loadPath = "/Users/mehulmathur/Desktop/Main Folder 0/Python Files/srfp/data_files/json_data/graph_data_isb_pullela_0.dat"; // IIITH_arnd.dat";
        // Load();

        // StartCoroutine(LoadGraphCoroutine());

        // RebuildGraph();
    }

    // {
    //     Nodes = RoadGraphSerializer.LoadGraph(loadPath);
    //     RoadGraphSerializer.SaveGraph(Nodes, Application.streamingAssetsPath + "/test.dat");
    //     // RebuildGraph();
    // }
    public IEnumerator LoadGraphCoroutine()
    {
        bool done = false;
        yield return RoadGraphSerializer.LoadGraphCoroutine(loadedNodes =>
        {
            Nodes = loadedNodes;
            done = true;
        });

        yield return null;

        if (Nodes == null || Nodes.Count == 0)
        {
            Debug.Log("RoadGraph failed to load.");
        }
        else
        {
            Debug.Log("RoadGraph loaded with " + Nodes.Count + " nodes.");
        }
    }

    public LaneNode GetClosestNode(Vector3 position, float maxDistance = 2f)
    {
        LaneNode closestNode = null;
        float closestDistSq = maxDistance * maxDistance; // use squared distance for efficiency

        foreach (var node in Nodes)
        {
            float distSq = (node.Position - position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestNode = node;
                closestDistSq = distSq;
            }
            // Debug.Log(distSq + "dist");
        }

        // Debug.Log(closestDistSq);
        return closestNode;
    }

    public List<LaneNode> GetNearbyNodes(Vector3 fromPosition, Vector3 toPosition, float radius = 8f, float angleThresh = 0.7f)
    {
        List<LaneNode> nearbyNodes = new List<LaneNode>();
        Vector3 orientation = toPosition - fromPosition;

        foreach (var node in Nodes)
        {
            Vector3 outVector = node.Position - toPosition;
            if (outVector.sqrMagnitude < radius * radius && Vector3.Dot(outVector.normalized, orientation) > angleThresh)
            {
                nearbyNodes.Add(node);
            }
        }

        return nearbyNodes;
    }

    public List<LaneNode> GetNodesFacing(List<LaneNode> prospectNodes, Vector3 direction, float thresh = 0)
    {
        List<LaneNode> nodesFacing = new List<LaneNode>();
        direction = direction.normalized;

        foreach (var node in prospectNodes)
        {
            Vector3 myDir;
            if (node.Incoming.Count > 0) myDir = (node.Position - node.Incoming[0].Position).normalized;
            else if (node.Outgoing.Count > 0) myDir = (node.Outgoing[0].Position - node.Position).normalized;
            else continue;

            if (Vector3.Dot(myDir, direction) > thresh) nodesFacing.Add(node);
        }

        return nodesFacing;
    }
    public void Clear()
    {
        Nodes.Clear();
    }
    // Rebuilds entire graph
    public void RebuildGraph()
    {
        // Nodes.Clear();
        var roadToNodes = new Dictionary<RoadGenerator, LaneNode[][]>();

        // Shape: [timeStep][laneIdx]

        // Gather all nodes per road
        foreach (var rg in FindObjectsOfType<RoadGenerator>())
        {
            int timeSteps = rg.pLanes.Count;
            int numLanes = rg.pLanes[0].Count;

            var nodes = new LaneNode[timeSteps][];
            if (GetClosestNode(rg.pLanes[0][0], 0.05f) != null) continue;
            for (int i = 0; i < timeSteps; i++)
            {
                nodes[i] = new LaneNode[numLanes];
                for (int j = 0; j < numLanes; j++)
                {
                    var node = new LaneNode { Position = rg.pLanes[i][j] };
                    Nodes.Add(node);
                    nodes[i][j] = node;
                }
            }

            roadToNodes[rg] = nodes;
        }

        // Link nodes along the lane, per road
        foreach (var kv in roadToNodes)
        {
            var rg = kv.Key;
            var nodes = kv.Value;

            int timeSteps = nodes.Length;
            int numLanes = nodes[0].Length;
            int n = rg.n_lanes;

            for (int j = 0; j < numLanes; j++)
            {
                bool isRightLane = rg.bidirectional && j >= n;
                for (int i = 0; i < timeSteps - 1; i++)
                {
                    LaneNode from, to;
                    if (!rg.bidirectional || !isRightLane)
                    {
                        // Forward direction: i → i+1
                        from = nodes[i][j];
                        to = nodes[i + 1][j];
                    }
                    else
                    {
                        // Reverse direction: i+1 → i
                        from = nodes[i + 1][j];
                        to = nodes[i][j];
                    }

                    from.Outgoing.Add(to);
                }
            }


        }

        // (Later Maybe) Cross-road/intersection edges as before...
        //     Use the IntersectionRecord to link end-points across roads

        /*
        foreach (var inter in FindObjectsOfType<IntersectionRecord>())
        {
            var lists = inter.ConnectedNodes; // List<List<LaneNode>>
            for (int i = 0; i < lists.Count; i++)
            for (int j = 0; j < lists.Count; j++)
            {
                if (i == j) continue;
                foreach (var src in lists[i])
                    foreach (var dst in lists[j])
                        src.Outgoing.Add(dst);
            }
        }
        */
    }

    // (Debuggin) gizmo-draw the graph
    int count = 0;
    bool done = false;
    void OnDrawGizmos()
    {
        // RebuildGraph();
        Gizmos.color = Color.yellow;
        foreach (var node in Nodes)
        {
            // Debug.Log(node.Position + "node");
            Gizmos.DrawSphere(node.Position + Vector3.up * 0.2f, 0.1f);
            foreach (var to in node.Outgoing)
            {
                count++;
                // Debug.Log(to.Position + "Outgoes");
                DrawArrow(node.Position + Vector3.up * 0.2f, to.Position + Vector3.up * 0.2f, Color.red);
            }
        }
        if (!done)
        {
            Debug.Log(count + " = count");
            done = true;
        }
    }


    void DrawArrow(Vector3 from, Vector3 to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(from, to);

        // arrowhead
        Vector3 direction = (to - from).normalized;
        float arrowHeadLength = 1f;
        float arrowHeadAngle = 20f;

        if (direction == Vector3.zero) return;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

        Gizmos.DrawLine(to, to + right * arrowHeadLength);
        Gizmos.DrawLine(to, to + left * arrowHeadLength);
    }
    // void OnApplicationQuit()
    // {
    //     RoadGraphSerializer.SaveGraph(Nodes);
    // }
    void OnDestroy()
    {
        Nodes.Clear();
    }
    void OnDisable()
    {
        Nodes.Clear();
    }


}
