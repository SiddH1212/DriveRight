using UnityEngine;


[ExecuteAlways]
public class Viewer : MonoBehaviour
{
    public RoadGraph graph;

    private void OnDrawGizmos()
    {
        if (graph == null) return;

        Gizmos.color = Color.yellow;
        foreach (var node in graph.Nodes)
        {
            Gizmos.DrawSphere(node.Position, 0.2f);
            foreach (var neighbor in node.Outgoing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(node.Position, neighbor.Position);
            }
        }
    }
    // void OnValidate()
    // {
    //     if (graph == null)
    //         graph = RoadGraph.Instance;
    // }

}
