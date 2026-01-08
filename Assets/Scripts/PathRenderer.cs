using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PathRenderer : MonoBehaviour
{
    public Material pathMaterial;
    public float pathWidth = 5f;
    public string minimapLayerName = "Path";

    private Mesh pathMesh;
    private MeshFilter meshFilter;

    // Reused buffers (NO allocations per frame)
    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();

    private int lastPathHash = -1;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        pathMesh = new Mesh();
        pathMesh.name = "PathMesh";
        pathMesh.MarkDynamic();  
        meshFilter.mesh = pathMesh;

        var renderer = GetComponent<MeshRenderer>();
        renderer.material = pathMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 10;

        gameObject.layer = LayerMask.NameToLayer(minimapLayerName);
    }

    public void DrawWorldPath(List<LaneNode> path, Vector3 startPosition)
    {
        if (path == null || path.Count < 1)
            return;

        // ---- Path hash: skip rebuild if unchanged ----
        int hash = path.Count;
        for (int i = 0; i < path.Count; i++)
            hash = hash * 31 + path[i].Position.GetHashCode();

        if (hash == lastPathHash)
            return;

        lastPathHash = hash;

        vertices.Clear();
        triangles.Clear();

        Vector3 prev = startPosition;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 curr = path[i].Position;
            Vector3 forward = (curr - prev).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward) * (pathWidth * 0.5f);

            vertices.Add(prev - right + Vector3.up * 0.05f);
            vertices.Add(prev + right + Vector3.up * 0.05f);

            if (i > 0)
            {
                int idx = vertices.Count - 4;

                triangles.Add(idx);
                triangles.Add(idx + 2);
                triangles.Add(idx + 1);

                triangles.Add(idx + 1);
                triangles.Add(idx + 2);
                triangles.Add(idx + 3);
            }

            prev = curr;
        }

        pathMesh.Clear();
        pathMesh.SetVertices(vertices);
        pathMesh.SetTriangles(triangles, 0);
        // NO normals (use Unlit material)
    }

    public void ClearPath()
    {
        pathMesh.Clear();
        lastPathHash = -1;
    }
}
