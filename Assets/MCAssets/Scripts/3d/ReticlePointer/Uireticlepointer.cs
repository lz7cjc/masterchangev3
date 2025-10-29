using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space reticle that ALWAYS appears in front of all objects.
/// Provides full customization of size, color, and appearance.
/// Works like a laser pointer - always visible at screen center.
/// </summary>
public class UIReticlePointer : MonoBehaviour
{
    [Header("Reticle Appearance")]
    [SerializeField] private ReticleShape defaultShape = ReticleShape.Circle;
    [SerializeField] private float dotSize = 15f; // Pixels
    [SerializeField] private float circleSize = 20f; // Pixels
    [SerializeField] private float ringThickness = 4f; // Pixels

    [Header("Size Adjustment")]
    [Tooltip("Scale reticle based on screen DPI for consistency across devices")]
    [SerializeField] private bool useDPIScaling = true;
    [Tooltip("Target DPI for size calculations (160 = standard Android)")]
    [SerializeField] private float targetDPI = 160f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.8f); // White with transparency
    [SerializeField] private Color hoverColor = new Color(0f, 1f, 0f, 1f); // Green when hovering
    [SerializeField] private Color activeColor = new Color(1f, 0.65f, 0f, 1f); // Orange when clicking

    [Header("Animation")]
    [SerializeField] private bool animateOnHover = true;
    [SerializeField] private float hoverScaleMultiplier = 1.3f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    public enum ReticleShape { Dot, Circle, Ring }

    // UI Elements
    private Canvas canvas;
    private GameObject reticleContainer;
    private Image dotImage;
    private Image circleImage;
    private Image ringOuterImage;
    private Image ringInnerImage;

    // State
    private bool isHovering = false;
    private bool isActive = false;
    private ReticleShape currentShape;
    private Vector3 targetScale = Vector3.one;
    private Color targetColor;

    #region Initialization

    private void Awake()
    {
        CreateUICanvas();
        CreateReticleElements();

        // FIXED: Initialize state properly
        currentShape = defaultShape;
        targetColor = idleColor;
        targetScale = Vector3.one;
        isHovering = false;
        isActive = false;

        UpdateReticleVisibility();

        // FIXED: Apply initial color immediately
        if (dotImage != null) dotImage.color = idleColor;
        if (circleImage != null) circleImage.color = idleColor;
        if (ringOuterImage != null) ringOuterImage.color = hoverColor;

        if (debugMode)
            Debug.Log("[UIReticlePointer] Initialized - Reticle should be visible");
    }

    private void Start()
    {
        // FIXED: Ensure reticle is visible on start
        // This runs after all Awake() calls, ensuring proper initialization order
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }

        // Set initial idle state
        SetHoverState(false);

        if (debugMode)
        {
            Debug.Log($"[UIReticlePointer] Start complete - Canvas active: {canvas.gameObject.activeSelf}");
            Debug.Log($"[UIReticlePointer] Screen DPI: {Screen.dpi}, Resolution: {Screen.width}x{Screen.height}");
        }
    }

    private void CreateUICanvas()
    {
        // Create a screen-space overlay canvas
        GameObject canvasObj = new GameObject("ReticleCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Always on top

        // FIXED: Use ConstantPixelSize for consistent sizing across resolutions
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f; // 1:1 pixel mapping

        canvasObj.AddComponent<GraphicRaycaster>(); // For UI interactions if needed

        if (debugMode)
            Debug.Log("[UIReticlePointer] Canvas created with ConstantPixelSize mode");
    }

    private void CreateReticleElements()
    {
        // Container for all reticle elements (centered on screen)
        reticleContainer = new GameObject("ReticleContainer");
        reticleContainer.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = reticleContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;

        // Create dot
        CreateDot();

        // Create circle
        CreateCircle();

        // Create ring
        CreateRing();

        if (debugMode)
            Debug.Log("[UIReticlePointer] All reticle elements created");
    }

    private void CreateDot()
    {
        GameObject dotObj = new GameObject("Dot");
        dotObj.transform.SetParent(reticleContainer.transform, false);

        RectTransform rect = dotObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // FIXED: Apply DPI scaling
        float scaledDotSize = GetScaledSize(dotSize);
        rect.sizeDelta = new Vector2(scaledDotSize, scaledDotSize);

        dotImage = dotObj.AddComponent<Image>();
        dotImage.sprite = CreateCircleSprite(64);
        dotImage.color = idleColor;
    }

    private void CreateCircle()
    {
        GameObject circleObj = new GameObject("Circle");
        circleObj.transform.SetParent(reticleContainer.transform, false);

        RectTransform rect = circleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // FIXED: Apply DPI scaling
        float scaledCircleSize = GetScaledSize(circleSize);
        rect.sizeDelta = new Vector2(scaledCircleSize, scaledCircleSize);

        circleImage = circleObj.AddComponent<Image>();
        circleImage.sprite = CreateCircleSprite(64);
        circleImage.color = idleColor;
    }

    private void CreateRing()
    {
        // Ring is made of two circles: outer (colored) and inner (transparent to cut out center)
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(reticleContainer.transform, false);

        RectTransform rect = ringObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // FIXED: Apply DPI scaling
        float scaledCircleSize = GetScaledSize(circleSize);
        rect.sizeDelta = new Vector2(scaledCircleSize, scaledCircleSize);

        ringOuterImage = ringObj.AddComponent<Image>();
        ringOuterImage.sprite = CreateRingSprite(64, ringThickness / circleSize);
        ringOuterImage.color = hoverColor;
    }

    /// <summary>
    /// Get size scaled for current screen DPI
    /// </summary>
    private float GetScaledSize(float baseSize)
    {
        if (!useDPIScaling)
            return baseSize;

        float currentDPI = Screen.dpi > 0 ? Screen.dpi : targetDPI;
        float dpiScale = currentDPI / targetDPI;

        if (debugMode)
            Debug.Log($"[UIReticlePointer] DPI scaling: {currentDPI} DPI, scale factor: {dpiScale}");

        return baseSize * dpiScale;
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        // Animate scale
        reticleContainer.transform.localScale = Vector3.Lerp(
            reticleContainer.transform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed
        );

        // Animate color
        Color currentColor = Color.Lerp(
            GetCurrentVisibleImage().color,
            targetColor,
            Time.deltaTime * animationSpeed
        );

        // Apply color to visible image
        if (dotImage.gameObject.activeSelf) dotImage.color = currentColor;
        if (circleImage.gameObject.activeSelf) circleImage.color = currentColor;
        if (ringOuterImage.gameObject.activeSelf) ringOuterImage.color = targetColor; // Ring always uses target color
    }

    #endregion

    #region Public API

    /// <summary>
    /// Call this when hovering over an interactive object
    /// </summary>
    public void SetHoverState(bool hovering)
    {
        if (isHovering == hovering) return;

        isHovering = hovering;

        if (hovering)
        {
            currentShape = ReticleShape.Ring;
            targetColor = hoverColor;

            if (animateOnHover)
            {
                targetScale = Vector3.one * hoverScaleMultiplier;
            }
        }
        else
        {
            currentShape = defaultShape;
            targetColor = idleColor;
            targetScale = Vector3.one;
        }

        UpdateReticleVisibility();

        if (debugMode)
            Debug.Log($"[UIReticlePointer] Hover state: {hovering}");
    }

    /// <summary>
    /// Call this when clicking/activating
    /// </summary>
    public void SetActiveState(bool active)
    {
        isActive = active;

        if (active)
        {
            targetColor = activeColor;
            targetScale = Vector3.one * 0.8f; // Shrink when clicking
        }
        else
        {
            targetColor = isHovering ? hoverColor : idleColor;
            targetScale = isHovering && animateOnHover ? Vector3.one * hoverScaleMultiplier : Vector3.one;
        }
    }

    /// <summary>
    /// Change the reticle shape
    /// </summary>
    public void SetReticleShape(ReticleShape shape)
    {
        defaultShape = shape;
        if (!isHovering)
        {
            currentShape = shape;
            UpdateReticleVisibility();
        }

        if (debugMode)
            Debug.Log($"[UIReticlePointer] Shape changed to: {shape}");
    }

    /// <summary>
    /// Change reticle colors
    /// </summary>
    public void SetColors(Color idle, Color hover, Color active)
    {
        idleColor = idle;
        hoverColor = hover;
        activeColor = active;
        targetColor = isHovering ? hoverColor : idleColor;
    }

    /// <summary>
    /// Change reticle sizes (in pixels)
    /// </summary>
    public void SetSizes(float dot, float circle, float ringThick)
    {
        dotSize = dot;
        circleSize = circle;
        ringThickness = ringThick;

        // FIXED: Apply DPI scaling when updating sizes
        float scaledDotSize = GetScaledSize(dotSize);
        float scaledCircleSize = GetScaledSize(circleSize);

        // Update UI element sizes
        if (dotImage != null)
        {
            RectTransform rect = dotImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(scaledDotSize, scaledDotSize);
        }

        if (circleImage != null)
        {
            RectTransform rect = circleImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(scaledCircleSize, scaledCircleSize);
        }

        if (ringOuterImage != null)
        {
            RectTransform rect = ringOuterImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(scaledCircleSize, scaledCircleSize);
            ringOuterImage.sprite = CreateRingSprite(64, ringThickness / circleSize);
        }

        if (debugMode)
            Debug.Log($"[UIReticlePointer] Sizes updated: dot={dot}px, circle={circle}px, ring={ringThick}px (scaled to {scaledDotSize}, {scaledCircleSize})");
    }

    /// <summary>
    /// Show or hide the entire reticle
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
    }

    #endregion

    #region Helper Methods

    private void UpdateReticleVisibility()
    {
        if (dotImage == null || circleImage == null || ringOuterImage == null) return;

        // Hide all first
        dotImage.gameObject.SetActive(false);
        circleImage.gameObject.SetActive(false);
        ringOuterImage.gameObject.SetActive(false);

        // Show only the current shape
        switch (currentShape)
        {
            case ReticleShape.Dot:
                dotImage.gameObject.SetActive(true);
                break;
            case ReticleShape.Circle:
                circleImage.gameObject.SetActive(true);
                break;
            case ReticleShape.Ring:
                ringOuterImage.gameObject.SetActive(true);
                break;
        }
    }

    private Image GetCurrentVisibleImage()
    {
        switch (currentShape)
        {
            case ReticleShape.Dot: return dotImage;
            case ReticleShape.Circle: return circleImage;
            case ReticleShape.Ring: return ringOuterImage;
            default: return circleImage;
        }
    }

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color fillColor = Color.white;
        Color clearColor = new Color(0, 0, 0, 0);

        float center = resolution / 2f;
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? fillColor : clearColor);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRingSprite(int resolution, float thicknessRatio)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color fillColor = Color.white;
        Color clearColor = new Color(0, 0, 0, 0);

        float center = resolution / 2f;
        float outerRadius = resolution / 2f;
        float innerRadius = outerRadius * (1f - thicknessRatio);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool inRing = distance <= outerRadius && distance >= innerRadius;
                texture.SetPixel(x, y, inRing ? fillColor : clearColor);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
    }

    #endregion

    #region Debug

    [ContextMenu("Test Hover On")]
    private void TestHoverOn() => SetHoverState(true);

    [ContextMenu("Test Hover Off")]
    private void TestHoverOff() => SetHoverState(false);

    [ContextMenu("Test Active")]
    private void TestActive()
    {
        SetActiveState(true);
        Invoke(nameof(TestInactive), 0.2f);
    }

    private void TestInactive() => SetActiveState(false);

    #endregion
}