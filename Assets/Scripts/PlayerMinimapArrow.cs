using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlayerMinimapArrow : MonoBehaviour
{
    public Transform player;
    public float size = 3f;                       // base authoring size (world units of the mesh)
    public string minimapLayerName = "Path";      // minimap only layer

    public Material circleMat;
    public Material arrowMat;

    [SerializeField] private float followOffset = 14f; // distance behind player in world space

    private Mesh arrowMesh;
    private GameObject bgCircle;

    // Remember the original localScale so we can set absolute scale cleanly
    private Vector3 initialLocalScale;

    void Awake()
    {
        initialLocalScale = transform.localScale;
    }

    void Start()
    {
        GenerateArrowMesh();
        CreateCircle();
        ApplyMaterialsAndLayers();
    }

    void LateUpdate()
    {
        if (!player) return;

        // World-space placement (independent of any scaling)
        transform.position = player.position + Vector3.up * 2f - player.forward * followOffset;

        // Face player direction
        Vector3 forward = player.forward;
        if (forward.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void CreateCircle()
    {
        bgCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgCircle.name = "MinimapArrowBackground";
        bgCircle.transform.SetParent(transform, false);

        bgCircle.transform.localPosition = new Vector3(0f, -0.01f, 0f); // tiny offset to avoid z-fighting
        bgCircle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face up
        bgCircle.transform.localScale = new Vector3(size * 2f, size * 2f, 1f);

        var col = bgCircle.GetComponent<Collider>();
        if (col) col.enabled = false;

        var mr = bgCircle.GetComponent<MeshRenderer>();
        mr.sharedMaterial = circleMat;
        bgCircle.layer = LayerMask.NameToLayer(minimapLayerName);
    }

    void ApplyMaterialsAndLayers()
    {
        gameObject.layer = LayerMask.NameToLayer(minimapLayerName);
        GetComponent<MeshRenderer>().sharedMaterial = arrowMat;
    }

    void GenerateArrowMesh()
    {
        // Build arrow in LOCAL space
        float tipLen   = size * 1.2f;
        float baseW    = size * 0.8f;
        float cutDepth = size * 0.2f;

        var vertices = new Vector3[5];
        var triangles = new int[6];

        vertices[0] = new Vector3(0f, 0f, tipLen);          // Tip
        vertices[1] = new Vector3(-baseW * 0.5f, 0f, 0f);   // Left base
        vertices[2] = new Vector3(0f, 0f, cutDepth);        // Inner cut
        vertices[3] = new Vector3( baseW * 0.5f, 0f, 0f);   // Right base
        vertices[4] = new Vector3(0f, 0f, 0f);              // Center base

        // ---- SHIFT to center the centroid at (0,0,0) ----

        Vector3 centroid = (vertices[0] + vertices[4]) / 2;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] -= centroid;
        // -------------------------------------------------

        // Triangles
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;

        if (arrowMesh != null) Destroy(arrowMesh);
        arrowMesh = new Mesh { name = "StylizedArrowMesh" };
        arrowMesh.SetVertices(vertices);
        arrowMesh.SetTriangles(triangles, 0);
        arrowMesh.RecalculateNormals();
        arrowMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = arrowMesh;
    }

    // Set absolute visual scale (relative to Start state). 1 = original size.
    public void SetAbsoluteScale(float scale)
    {
        transform.localScale = initialLocalScale * scale;

        // Keep the circle’s authored scale consistent with the mesh’s base size,
        // then rely on transform.localScale to up/down-scale both together.
        if (bgCircle != null)
        {
            bgCircle.transform.localScale = new Vector3(size * 2f, size * 2f, 1f);
        }
    }
}
