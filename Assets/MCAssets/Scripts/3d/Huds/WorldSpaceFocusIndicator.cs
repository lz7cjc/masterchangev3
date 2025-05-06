using UnityEngine;

public class WorldSpaceFocusIndicator : MonoBehaviour
{
    public enum FocusIndicatorShape { Square, Circle }

    // Scale factor to convert from user-friendly units to world units
    // 100 means 1 user unit = 0.01 world units
    [Header("Scale Settings")]
    [SerializeField] private float scaleFactor = 100f;

    [Header("Focus Indicator Settings")]
    [SerializeField] private float indicatorDistance = 40f; // Was 0.4
    [SerializeField] private FocusIndicatorShape defaultShape = FocusIndicatorShape.Square;
    [SerializeField] private float dotSize = 4f;           // Was 0.04
    [SerializeField] private float circleSize = 4f;        // Was 0.04
    [SerializeField] private float circleThickness = 1f;   // Was 0.01
    [SerializeField] private Color defaultColor = Color.yellow;
    [SerializeField] private Color interactiveColor = Color.green;

    // Added settings for enhanced rendering
    [SerializeField] private float minVisibleCircleSize = 0.5f;  // Was 0.005
    [SerializeField] private float thicknessScaleFactor = 0.25f;   // Controls how thickness scales with size

    [Header("Debug Options")]
    [SerializeField] private bool debugMode = false;

    private GameObject squareIndicator;
    private GameObject circleIndicator;
    private GameObject ringIndicator;
    private Camera mainCamera;
    private bool isOverInteractive = false;
    private LineRenderer lineRenderer;  // Keep reference to update thickness

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("WorldSpaceFocusIndicator: No camera found!");
                return;
            }
        }

        // Create all indicators
        CreateSquareIndicator();
        CreateSolidCircleIndicator();
        CreateRingIndicator();

        // Set initial visibility based on selected shape
        UpdateIndicatorVisibility();

        if (debugMode)
        {
            Debug.Log("WorldSpaceFocusIndicator initialized with shape: " + defaultShape);
        }
    }

    void CreateSquareIndicator()
    {
        squareIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        squareIndicator.name = "SquareIndicator";
        squareIndicator.transform.parent = transform;

        // Convert from user-friendly units to world units
        float worldDotSize = dotSize / scaleFactor;
        squareIndicator.transform.localScale = new Vector3(worldDotSize, worldDotSize, worldDotSize);

        // Configure renderer
        Renderer renderer = squareIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Unlit/Color"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Standard"));

        mat.color = defaultColor;
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Disable collider
        Destroy(squareIndicator.GetComponent<Collider>());
    }

    void CreateSolidCircleIndicator()
    {
        // Create a parent object for solid circle
        circleIndicator = new GameObject("SolidCircleIndicator");
        circleIndicator.transform.parent = transform;

        // Create a single disc for the solid circle
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "SolidCircle";
        disc.transform.parent = circleIndicator.transform;

        // Convert from user-friendly units to world units
        float worldCircleSize = circleSize / scaleFactor;

        // Scale to make it a flat disc and the right size
        disc.transform.localScale = new Vector3(worldCircleSize, 0.001f, worldCircleSize);

        // Rotate to face forward (cylinder's circular face is up/down by default)
        disc.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Configure renderer
        Renderer renderer = disc.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Unlit/Color"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Standard"));

        mat.color = defaultColor;
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Disable collider
        Destroy(disc.GetComponent<Collider>());
    }

    void CreateRingIndicator()
    {
        // Create a parent object for ring
        ringIndicator = new GameObject("RingIndicator");
        ringIndicator.transform.parent = transform;

        // We'll create a ring using a LineRenderer
        lineRenderer = ringIndicator.AddComponent<LineRenderer>();

        // Set up the line renderer for a ring
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;

        // Calculate proper thickness for current size
        float calculatedThickness = GetScaledThickness(circleSize);
        lineRenderer.widthMultiplier = calculatedThickness;

        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Create material for the ring
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Unlit/Color"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Standard"));

        mat.color = interactiveColor;
        lineRenderer.material = mat;

        // Create the points for the ring
        // Convert user units to world units
        float worldCircleSize = circleSize / scaleFactor;
        UpdateRingGeometry(worldCircleSize);
    }

    // New method to update ring geometry based on size
    private void UpdateRingGeometry(float worldSize)
    {
        if (lineRenderer == null) return;

        float worldMinSize = minVisibleCircleSize / scaleFactor;

        // For smaller circles, use more segments to maintain smoothness
        int segments = worldSize < worldMinSize * 3 ? 64 :
                      worldSize < worldMinSize * 5 ? 48 : 32;

        Vector3[] positions = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * worldSize;
            float y = Mathf.Cos(angle) * worldSize;
            positions[i] = new Vector3(x, y, 0);
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(positions);
    }

    // Helper method to calculate proper thickness for a given size
    private float GetScaledThickness(float size)
    {
        // Convert from user units to world units
        float worldSize = size / scaleFactor;
        float worldThickness = circleThickness / scaleFactor;
        float worldMinVisibleSize = minVisibleCircleSize / scaleFactor;

        // Improved thickness scaling for better visibility at small sizes
        // Base thickness is proportional to circle size with a minimum threshold
        float baseThicknessRatio = 0.1f; // 10% of circle size as base ratio
        float minThicknessRatio = 0.15f; // Minimum thickness as percentage of circle size

        // For very small circles, increase the proportion to maintain visibility
        if (size < 2.0f)
        {
            // Gradually increase thickness ratio for smaller circles
            float ratio = Mathf.Lerp(0.25f, minThicknessRatio, size / 2.0f);
            baseThicknessRatio = Mathf.Max(baseThicknessRatio, ratio);
        }

        // Calculate final thickness in world units
        float scaledThickness = worldSize * baseThicknessRatio;

        // Ensure minimum absolute thickness to prevent disappearing lines
        float minAbsoluteThickness = 0.2f / scaleFactor;
        return Mathf.Max(scaledThickness, minAbsoluteThickness);
    }

    void Update()
    {
        if (mainCamera == null || squareIndicator == null || circleIndicator == null || ringIndicator == null) return;

        // Calculate position in front of camera
        // Convert user-friendly distance to world units
        float worldDistance = indicatorDistance / scaleFactor;
        Vector3 position = mainCamera.transform.position +
                          mainCamera.transform.forward * worldDistance;

        // Only update the indicators, not the parent transform
        squareIndicator.transform.position = position;
        squareIndicator.transform.rotation = mainCamera.transform.rotation;

        circleIndicator.transform.position = position;
        circleIndicator.transform.rotation = mainCamera.transform.rotation;

        ringIndicator.transform.position = position;
        ringIndicator.transform.rotation = mainCamera.transform.rotation;

        // Raycast to check for interactable objects directly
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, 100f))
        {
            if (debugMode)
            {
                Debug.DrawLine(mainCamera.transform.position, hit.point, Color.red, 0.1f);
                Debug.Log("Hit object: " + hit.collider.gameObject.name +
                          " | isTrigger: " + hit.collider.isTrigger +
                          " | Has BoxCollider: " + (hit.collider.GetComponent<BoxCollider>() != null));
            }

            // Check if this is a trigger BoxCollider, which is what we want for interactive objects
            Collider hitCollider = hit.collider;
            bool isInteractive = false;

            if (hitCollider != null && hitCollider.GetType() == typeof(BoxCollider) && hitCollider.isTrigger)
            {
                isInteractive = true;

                if (debugMode)
                {
                    Debug.Log("Found interactive object: " + hit.collider.gameObject.name);
                }
            }

            SetInteractiveState(isInteractive);
        }
        else
        {
            SetInteractiveState(false);
        }
    }

    public void SetInteractiveState(bool interactive)
    {
        if (isOverInteractive == interactive) return;

        isOverInteractive = interactive;

        // Update indicator visibility and color
        UpdateIndicatorVisibility();

        if (debugMode)
        {
            Debug.Log("Setting interactive state: " + interactive);
        }
    }

    void UpdateIndicatorVisibility()
    {
        if (squareIndicator == null || circleIndicator == null || ringIndicator == null) return;

        if (isOverInteractive)
        {
            // When interactive, show only the ring indicator
            squareIndicator.SetActive(false);
            circleIndicator.SetActive(false);
            ringIndicator.SetActive(true);
        }
        else
        {
            // When not interactive, show either square or solid circle based on default shape
            squareIndicator.SetActive(defaultShape == FocusIndicatorShape.Square);
            circleIndicator.SetActive(defaultShape == FocusIndicatorShape.Circle);
            ringIndicator.SetActive(false);
        }
    }

    public void SetDefaultShape(FocusIndicatorShape shape)
    {
        defaultShape = shape;
        UpdateIndicatorVisibility();

        if (debugMode)
        {
            Debug.Log("Default shape changed to: " + shape);
        }
    }

    // Method to update circle size at runtime
    public void SetCircleSize(float newSize)
    {
        if (newSize <= 0) return;

        circleSize = newSize;
        float worldSize = newSize / scaleFactor;

        // Update the solid circle scale
        if (circleIndicator != null && circleIndicator.transform.childCount > 0)
        {
            GameObject disc = circleIndicator.transform.GetChild(0).gameObject;
            disc.transform.localScale = new Vector3(worldSize, 0.001f, worldSize);
        }

        // Update the ring
        if (lineRenderer != null)
        {
            // Update geometry and thickness
            UpdateRingGeometry(worldSize);
            lineRenderer.widthMultiplier = GetScaledThickness(newSize);
        }
    }
}