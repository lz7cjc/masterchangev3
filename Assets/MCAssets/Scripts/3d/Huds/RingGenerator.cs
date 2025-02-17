using UnityEngine;

public class RingGenerator : MonoBehaviour
{
    [Header("Ring Dimensions")]
    [SerializeField] private float outerRadius = 2f;
    [SerializeField] private float innerRadius = 1.8f;
    [SerializeField] private float depth = 1f;

    [Header("Ring Generation Settings")]
    [SerializeField] private int segments = 32;
    [SerializeField] private Material ringMaterial;
    [SerializeField] private bool autoRegenerate = true;

    // Public properties
    public float OuterRadius => outerRadius;
    public float InnerRadius => innerRadius;
    public float Depth => depth;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        GenerateRing();
    }

    private void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (ringMaterial != null)
            meshRenderer.material = ringMaterial;
    }

    private void OnValidate()
    {
        if (autoRegenerate && Application.isPlaying)
        {
            GenerateRing();
        }
    }

    public void GenerateRing()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Ring";

        CreateMeshGeometry(mesh);
        ApplyMeshToFilter(mesh);
    }

    private void CreateMeshGeometry(Mesh mesh)
    {
        // Calculate vertices for top and bottom rings
        int verticesPerRing = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[verticesPerRing * 2];
        Vector2[] uvs = new Vector2[verticesPerRing * 2];

        GenerateRingVertices(vertices, uvs, verticesPerRing);
        int[] triangles = GenerateTriangles(verticesPerRing);

        ApplyMeshData(mesh, vertices, triangles, uvs);
    }

    private void GenerateRingVertices(Vector3[] vertices, Vector2[] uvs, int verticesPerRing)
    {
        // Generate top ring vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (2 * Mathf.PI * i) / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            // Inner vertex
            vertices[i * 2] = new Vector3(x * innerRadius, depth / 2, z * innerRadius);
            uvs[i * 2] = new Vector2((float)i / segments, 1f);

            // Outer vertex
            vertices[i * 2 + 1] = new Vector3(x * outerRadius, depth / 2, z * outerRadius);
            uvs[i * 2 + 1] = new Vector2((float)i / segments, 0f);
        }

        // Generate bottom ring vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (2 * Mathf.PI * i) / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            // Inner vertex
            vertices[verticesPerRing + i * 2] = new Vector3(x * innerRadius, -depth / 2, z * innerRadius);
            uvs[verticesPerRing + i * 2] = new Vector2((float)i / segments, 1f);

            // Outer vertex
            vertices[verticesPerRing + i * 2 + 1] = new Vector3(x * outerRadius, -depth / 2, z * outerRadius);
            uvs[verticesPerRing + i * 2 + 1] = new Vector2((float)i / segments, 0f);
        }
    }

    private int[] GenerateTriangles(int verticesPerRing)
    {
        int[] triangles = new int[segments * 24];
        int triangleIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int topStart = i * 2;
            int bottomStart = verticesPerRing + i * 2;

            // Inner wall
            AddTriangle(triangles, ref triangleIndex,
                bottomStart, topStart + 2, topStart,
                bottomStart, bottomStart + 2, topStart + 2);

            // Outer wall
            AddTriangle(triangles, ref triangleIndex,
                topStart + 1, topStart + 3, bottomStart + 1,
                bottomStart + 1, topStart + 3, bottomStart + 3);

            // Top face
            AddTriangle(triangles, ref triangleIndex,
                topStart, topStart + 2, topStart + 1,
                topStart + 1, topStart + 2, topStart + 3);

            // Bottom face
            AddTriangle(triangles, ref triangleIndex,
                bottomStart, bottomStart + 1, bottomStart + 2,
                bottomStart + 1, bottomStart + 3, bottomStart + 2);
        }

        return triangles;
    }

    private void AddTriangle(int[] triangles, ref int index,
        int a1, int a2, int a3, int b1, int b2, int b3)
    {
        triangles[index++] = a1;
        triangles[index++] = a2;
        triangles[index++] = a3;
        triangles[index++] = b1;
        triangles[index++] = b2;
        triangles[index++] = b3;
    }

    private void ApplyMeshData(Mesh mesh, Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    private void ApplyMeshToFilter(Mesh mesh)
    {
        if (meshFilter != null)
        {
            meshFilter.mesh = mesh;
        }
    }

    // Public method to force regeneration
    public void RegenerateRing()
    {
        GenerateRing();
    }
}