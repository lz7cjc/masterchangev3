using UnityEngine;

public class WorldSpaceFocusIndicator : MonoBehaviour
{
    public enum FocusIndicatorShape { Square, Circle }

    [Header("Focus Indicator Settings")]
    [SerializeField] private float indicatorDistance = 10f;
    [SerializeField] private FocusIndicatorShape defaultShape = FocusIndicatorShape.Square;
    [SerializeField] private float dotSize = 0.05f; // Increased default size
    [SerializeField] private float circleSize = 0.055f; // 10% larger than dot
    [SerializeField] private float circleThickness = 0.005f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color interactiveColor = Color.green;

    private GameObject focusIndicator;
    private Camera mainCamera;
    private bool isOverInteractive = false;

    void Start()
    {
        mainCamera = Camera.main;
        CreateFocusIndicator();
    }

    void CreateFocusIndicator()
    {
        focusIndicator = new GameObject("FocusIndicator");
        MeshFilter meshFilter = focusIndicator.AddComponent<MeshFilter>();
        MeshRenderer renderer = focusIndicator.AddComponent<MeshRenderer>();

        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        UpdateMesh();
    }

    void Update()
    {
        UpdateFocusIndicatorPosition();
        UpdateFocusIndicatorAppearance();
    }

    void UpdateMesh()
    {
        Mesh mesh = new Mesh();
        MeshFilter meshFilter = focusIndicator.GetComponent<MeshFilter>();
        MeshRenderer renderer = focusIndicator.GetComponent<MeshRenderer>();

        if (isOverInteractive)
        {
            // Create circle mesh (outline)
            mesh = CreateCircleMesh(circleSize, circleThickness);
        }
        else
        {
            // Create dot mesh based on default shape
            mesh = defaultShape == FocusIndicatorShape.Square
                ? CreateSquareMesh(dotSize)
                : CreateDotMesh(dotSize);
        }

        meshFilter.mesh = mesh;
        renderer.material.color = isOverInteractive ? interactiveColor : defaultColor;
    }

    Mesh CreateSquareMesh(float size)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] uvs = new Vector2[4];
        int[] triangles = new int[6];

        float halfSize = size / 2f;

        vertices[0] = new Vector3(-halfSize, -halfSize, 0);
        vertices[1] = new Vector3(halfSize, -halfSize, 0);
        vertices[2] = new Vector3(-halfSize, halfSize, 0);
        vertices[3] = new Vector3(halfSize, halfSize, 0);

        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);

        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh CreateDotMesh(float size)
    {
        // Implement a circular dot using CreateCircleMesh with thickness matching the radius
        return CreateCircleMesh(size / 2f, size / 2f);
    }

    Mesh CreateCircleMesh(float outerRadius, float thickness)
    {
        Mesh mesh = new Mesh();
        int segments = 36;

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        float innerRadius = outerRadius - thickness;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;

            // Outer circle vertices
            vertices[i * 2] = new Vector3(
                Mathf.Cos(angle) * outerRadius,
                Mathf.Sin(angle) * outerRadius,
                0
            );

            // Inner circle vertices
            vertices[i * 2 + 1] = new Vector3(
                Mathf.Cos(angle) * innerRadius,
                Mathf.Sin(angle) * innerRadius,
                0
            );

            // UV coordinates
            uvs[i * 2] = new Vector2(1, 1);
            uvs[i * 2 + 1] = new Vector2(0, 0);
        }

        // Create triangles
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * 6;
            int vertIndex = i * 2;

            triangles[baseIndex] = vertIndex;
            triangles[baseIndex + 1] = vertIndex + 2;
            triangles[baseIndex + 2] = vertIndex + 1;

            triangles[baseIndex + 3] = vertIndex + 1;
            triangles[baseIndex + 4] = vertIndex + 2;
            triangles[baseIndex + 5] = vertIndex + 3;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    void UpdateFocusIndicatorPosition()
    {
        if (focusIndicator == null) return;

        Vector3 indicatorPosition = mainCamera.transform.position +
                                    mainCamera.transform.forward * indicatorDistance;

        focusIndicator.transform.position = indicatorPosition;
        focusIndicator.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
    }

    void UpdateFocusIndicatorAppearance()
    {
        if (focusIndicator == null) return;

        UpdateMesh();
    }

    public void SetInteractiveState(bool interactive)
    {
        if (isOverInteractive != interactive)
        {
            isOverInteractive = interactive;
            UpdateMesh();
        }
    }
}