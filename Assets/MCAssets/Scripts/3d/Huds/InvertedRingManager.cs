using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class InvertedRingGenerator : MonoBehaviour
{
    [Header("Ring Dimensions")]
    [SerializeField] private float innerRadius = 2f;
    [SerializeField] private float outerRadius = 2.5f;
    [SerializeField] private int segments = 32;

    [Header("Ring Appearance")]
    [SerializeField] private Material ringMaterial;
    [SerializeField] private bool smoothNormals = true;

    private Mesh ringMesh;

    public float InnerRadius => innerRadius;

    private void Awake()
    {
        GenerateRing();
    }

    private void GenerateRing()
    {
        ringMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = ringMesh;

        if (ringMaterial != null)
        {
            GetComponent<MeshRenderer>().material = ringMaterial;
        }

        // Calculate vertices
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        Vector3[] normals = new Vector3[(segments + 1) * 2];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(x * innerRadius, 0, z * innerRadius);
            vertices[i * 2 + 1] = new Vector3(x * outerRadius, 0, z * outerRadius);

            // Invert normals to make the ring visible from the inside
            Vector3 normal = smoothNormals ?
                new Vector3(-x, 0, -z) :
                Vector3.down;

            normals[i * 2] = normal;
            normals[i * 2 + 1] = normal;

            uvs[i * 2] = new Vector2((float)i / segments, 0);
            uvs[i * 2 + 1] = new Vector2((float)i / segments, 1);
        }

        // Calculate triangles
        int[] triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int indexOffset = i * 6;
            int vertexOffset = i * 2;

            triangles[indexOffset] = vertexOffset;
            triangles[indexOffset + 1] = vertexOffset + 1;
            triangles[indexOffset + 2] = vertexOffset + 2;

            triangles[indexOffset + 3] = vertexOffset + 1;
            triangles[indexOffset + 4] = vertexOffset + 3;
            triangles[indexOffset + 5] = vertexOffset + 2;
        }

        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.uv = uvs;
        ringMesh.normals = normals;
        ringMesh.RecalculateBounds();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && ringMesh != null)
        {
            GenerateRing();
        }
    }
}