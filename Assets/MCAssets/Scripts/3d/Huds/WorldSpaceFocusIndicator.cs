using UnityEngine;

public class WorldSpaceFocusIndicator : MonoBehaviour
{
    public enum FocusIndicatorShape { Square, Circle }

    [Header("Focus Indicator Settings")]
    [SerializeField] private float indicatorDistance = 0.4f;
    [SerializeField] private FocusIndicatorShape defaultShape = FocusIndicatorShape.Square;
    [SerializeField] private float dotSize = 0.04f;
    [SerializeField] private float circleSize = 0.04f;
    [SerializeField] private float circleThickness = 0.01f;
    [SerializeField] private Color defaultColor = Color.yellow;
    [SerializeField] private Color interactiveColor = Color.green;

    [Header("Debug Options")]
    [SerializeField] private bool debugMode = false;

    private GameObject squareIndicator;
    private GameObject circleIndicator;
    private GameObject ringIndicator;
    private Camera mainCamera;
    private bool isOverInteractive = false;

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
        squareIndicator.transform.localScale = new Vector3(dotSize, dotSize, dotSize);

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

        // Scale to make it a flat disc and the right size
        disc.transform.localScale = new Vector3(circleSize, 0.001f, circleSize);

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
        LineRenderer lineRenderer = ringIndicator.AddComponent<LineRenderer>();

        // Set up the line renderer for a ring
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = circleThickness;
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

        // Create the circular points for the ring
        int segments = 32;
        Vector3[] positions = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * circleSize;
            float y = Mathf.Cos(angle) * circleSize;
            positions[i] = new Vector3(x, y, 0);
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(positions);
    }

    void Update()
    {
        if (mainCamera == null || squareIndicator == null || circleIndicator == null || ringIndicator == null) return;

        // Calculate position in front of camera
        Vector3 position = mainCamera.transform.position +
                          mainCamera.transform.forward * indicatorDistance;

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
}