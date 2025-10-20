using UnityEngine;

public class SpriteBasedFocusIndicator : MonoBehaviour
{
    public enum FocusIndicatorShape { Square, Circle }

    [Header("Focus Indicator Settings")]
    [SerializeField] private float indicatorDistance = 35f;
    [SerializeField] private FocusIndicatorShape defaultShape = FocusIndicatorShape.Circle;

    [Header("Size Settings")]
    [SerializeField] private float squareSize = 1.5f;  // Size when Default Shape = Square
    [SerializeField] private float circleSize = 1.2f;  // Size when Default Shape = Circle (also base for ring)
    [SerializeField] private float ringThickness = 0.4f;

    [Header("Color Settings")]
    [SerializeField] private Color defaultColor = Color.yellow;
    [SerializeField] private Color interactiveColor = Color.green;

    [Header("Debug Options")]
    [SerializeField] private bool debugMode = false;

    // References to the sprite game objects
    private GameObject squareIndicator;
    private GameObject circleIndicator;
    private GameObject ringIndicator;
    private Camera mainCamera;
    private bool isOverInteractive = false;

    // Sprite references - assign these in Unity Editor
    [Header("Sprite References")]
    [SerializeField] private Sprite squareSprite; // A simple square sprite
    [SerializeField] private Sprite circleSprite; // A filled circle sprite
    [SerializeField] private Sprite ringSprite;   // A hollow ring sprite

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("SpriteBasedFocusIndicator: No camera found!");
                return;
            }
        }

        // Create sprites if not provided
        if (squareSprite == null)
            squareSprite = CreateDefaultSprite(16, 16, true);
        if (circleSprite == null)
            circleSprite = CreateCircleSprite(32, true);
        if (ringSprite == null)
            ringSprite = CreateRingSprite(32, 0.7f);

        // Create all indicators
        CreateSquareIndicator();
        CreateCircleIndicator();
        CreateRingIndicator();

        // Set initial visibility based on selected shape
        UpdateIndicatorVisibility();

        if (debugMode)
        {
            Debug.Log("SpriteBasedFocusIndicator initialized with shape: " + defaultShape);
        }
    }

    // ADD THIS METHOD - This is called whenever Inspector values change
    void OnValidate()
    {
        // Only update if we're in play mode and indicators exist
        if (Application.isPlaying && squareIndicator != null && circleIndicator != null && ringIndicator != null)
        {
            UpdateIndicatorSizes();
            UpdateIndicatorVisibility();
        }
    }

    // ADD THIS METHOD - Updates all indicator sizes
    void UpdateIndicatorSizes()
    {
        if (squareIndicator != null)
        {
            squareIndicator.transform.localScale = new Vector3(squareSize, squareSize, 1f);
        }

        if (circleIndicator != null)
        {
            circleIndicator.transform.localScale = new Vector3(circleSize, circleSize, 1f);
        }

        if (ringIndicator != null)
        {
            float ringSize = circleSize * (1f + ringThickness);
            ringIndicator.transform.localScale = new Vector3(ringSize, ringSize, 1f);
        }

        if (debugMode)
        {
            Debug.Log($"Updated indicator sizes - Square: {squareSize}, Circle: {circleSize}, Ring thickness: {ringThickness}");
        }
    }

    void CreateSquareIndicator()
    {
        squareIndicator = new GameObject("SquareIndicator");
        squareIndicator.transform.parent = transform;

        SpriteRenderer renderer = squareIndicator.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.color = defaultColor;

        // Set size
        squareIndicator.transform.localScale = new Vector3(squareSize, squareSize, 1f);
    }

    void CreateCircleIndicator()
    {
        circleIndicator = new GameObject("CircleIndicator");
        circleIndicator.transform.parent = transform;

        SpriteRenderer renderer = circleIndicator.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.color = defaultColor;

        // Set size
        circleIndicator.transform.localScale = new Vector3(circleSize, circleSize, 1f);
    }

    void CreateRingIndicator()
    {
        ringIndicator = new GameObject("RingIndicator");
        ringIndicator.transform.parent = transform;

        SpriteRenderer renderer = ringIndicator.AddComponent<SpriteRenderer>();
        renderer.sprite = ringSprite;
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.color = interactiveColor;

        // Set size - slightly larger to account for ring thickness
        float ringSize = circleSize * (1f + ringThickness);
        ringIndicator.transform.localScale = new Vector3(ringSize, ringSize, 1f);
    }

    // Helper method to create a default square sprite
    private Sprite CreateDefaultSprite(int width, int height, bool filled)
    {
        Texture2D texture = new Texture2D(width, height);
        Color fillColor = Color.white;
        Color clearColor = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (filled || x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    texture.SetPixel(x, y, fillColor);
                }
                else
                {
                    texture.SetPixel(x, y, clearColor);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // Helper method to create a circle sprite
    private Sprite CreateCircleSprite(int size, bool filled)
    {
        Texture2D texture = new Texture2D(size, size);
        Color fillColor = Color.white;
        Color clearColor = new Color(0, 0, 0, 0);

        float radius = size / 2f;
        float radiusSquared = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float distanceSquared = dx * dx + dy * dy;

                if (filled)
                {
                    // Filled circle
                    if (distanceSquared <= radiusSquared)
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, clearColor);
                    }
                }
                else
                {
                    // Outline only
                    float innerRadiusSquared = (radius - 1) * (radius - 1);
                    if (distanceSquared <= radiusSquared && distanceSquared >= innerRadiusSquared)
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, clearColor);
                    }
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Helper method to create a ring sprite
    private Sprite CreateRingSprite(int size, float thickness)
    {
        Texture2D texture = new Texture2D(size, size);
        Color fillColor = Color.white;
        Color clearColor = new Color(0, 0, 0, 0);

        float radius = size / 2f;
        float radiusSquared = radius * radius;
        float innerRadius = radius * (1f - thickness);
        float innerRadiusSquared = innerRadius * innerRadius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= radiusSquared && distanceSquared >= innerRadiusSquared)
                {
                    texture.SetPixel(x, y, fillColor);
                }
                else
                {
                    texture.SetPixel(x, y, clearColor);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

        // Raycast check for interactable objects
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

    // UPDATED: Method to directly set circle size
    public void SetCircleSize(float size)
    {
        if (size <= 0) return;

        circleSize = size;
        UpdateIndicatorSizes(); // Use the centralized method
    }

    // UPDATED: Method to set ring thickness
    public void SetRingThickness(float thickness)
    {
        if (thickness <= 0 || thickness >= 1) return;

        ringThickness = thickness;
        UpdateIndicatorSizes(); // Use the centralized method

        // Recreate the ring sprite with new thickness
        ringSprite = CreateRingSprite(32, thickness);
        if (ringIndicator != null)
        {
            SpriteRenderer renderer = ringIndicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = ringSprite;
            }
        }
    }

    // ADD THIS METHOD: Method to set square size
    public void SetSquareSize(float size)
    {
        if (size <= 0) return;

        squareSize = size;
        UpdateIndicatorSizes(); // Use the centralized method
    }
}