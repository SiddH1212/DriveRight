using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public RoadGraph roadGraph;
    private LaneNode debugStartNode, debugEndNode;
    private List<LaneNode> debugPath;

    // Gets a path from the startPos to the endPos
    public List<LaneNode> GetPath(Vector3 startPos, Vector3 endPos)
    {
        List<LaneNode> startCandidates = roadGraph.GetClosestNodes(startPos, 10, 5f);
        List<LaneNode> endCandidates = roadGraph.GetClosestNodes(endPos, 10, 100f);

        debugStartNode = null;
        debugEndNode = null;
        debugPath = null;

        float bestScore = float.MaxValue;
        List<LaneNode> bestPath = null;

        foreach (var start in startCandidates)
        {
            foreach (var end in endCandidates)
            {
                var path = AStar(start, end);
                if (path != null && path.Count > 0)
                {
                    float pathLength = PathDistance(path);
                    float startProximity = Vector3.Distance(start.Position, startPos);

                    // We can modify this weight to balance favoring closer start nodes
                    float weight = 10.0f;
                    float score = pathLength + weight * startProximity;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPath = path;
                        debugStartNode = start;
                        debugEndNode = end;
                    }
                }
            }
        }

        if (bestPath != null)
        {
            debugPath = bestPath;
            return bestPath;
        }

        Debug.LogWarning("Path not found between any start/end node pair.");
        return null;
    }

    private float PathDistance(List<LaneNode> path)
    {
        float dist = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            dist += Vector3.Distance(path[i - 1].Position, path[i].Position);
        }
        return dist;
    }

    // A* algo for pathfinding
    private List<LaneNode> AStar(LaneNode start, LaneNode goal)
    {
        var openSet = new PriorityQueue<LaneNode>();
        var cameFrom = new Dictionary<LaneNode, LaneNode>();
        var gScore = new Dictionary<LaneNode, float>();
        var fScore = new Dictionary<LaneNode, float>();

        openSet.Enqueue(start, 0f);
        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            LaneNode current = openSet.Dequeue();

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (LaneNode neighbor in current.Outgoing)
            {
                float tentativeGScore = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        // Debug.LogWarning("Path not found.");
        return new List<LaneNode>();
    }

    private float Heuristic(LaneNode a, LaneNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    private List<LaneNode> ReconstructPath(Dictionary<LaneNode, LaneNode> cameFrom, LaneNode current)
    {
        var totalPath = new List<LaneNode> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }

    void OnDrawGizmos()
    {
        if (debugStartNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(debugStartNode.Position + Vector3.up * 0.5f, 2f);
        }

        if (debugEndNode != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(debugEndNode.Position + Vector3.up * 0.5f, 2f);
        }

        if (debugPath != null && debugPath.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < debugPath.Count - 1; i++)
            {
                Vector3 from = debugPath[i].Position + Vector3.up * 0.1f;
                Vector3 to = debugPath[i + 1].Position + Vector3.up * 0.1f;
                Gizmos.DrawLine(from, to);
                Gizmos.DrawSphere(from, 0.07f);
            }

            // Draw last point
            Gizmos.DrawSphere(debugPath[debugPath.Count - 1].Position + Vector3.up * 0.1f, 0.07f);
        }
    }


}
