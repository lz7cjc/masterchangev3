using UnityEngine;

/// <summary>
/// Visual countdown feedback for VR gaze interactions
/// Optimized for mobile performance
/// Uses 3D world-space ring for efficiency (no Canvas overhead)
/// </summary>
public class GazeHUDCountdown : MonoBehaviour
{
    [Header("Ring Settings (3D World Space)")]
    [SerializeField] private GameObject countdownRing;
    [SerializeField] private float ringMinScale = 0.04f;
    [SerializeField] private float ringMaxScale = 0.12f;
    [SerializeField] private Color ringStartColor = Color.white;
    [SerializeField] private Color ringCompleteColor = Color.green;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool pulseOnComplete = true;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private bool rotateRing = true;
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Position")]
    [SerializeField] private bool followReticle = true;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float distanceFromCamera = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Private state
    private float currentProgress = 0f;
    private bool isCountingDown = false;
    private float countdownDuration = 3f;
    private Renderer ringRenderer;
    private Material ringMaterial;
    private bool isPulsing = false;
    private float pulseTimer = 0f;

    void Awake()
    {
        InitializeRing();
    }

    void Update()
    {
        if (isPulsing)
        {
            UpdatePulseAnimation();
        }

        if (followReticle && isCountingDown)
        {
            UpdateRingPosition();
        }

        if (rotateRing && isCountingDown)
        {
            UpdateRingRotation();
        }
    }

    /// <summary>
    /// Initialize 3D ring countdown
    /// </summary>
    private void InitializeRing()
    {
        // Find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                GazeReticlePointer pointer = FindFirstObjectByType<GazeReticlePointer>();
                if (pointer != null)
                {
                    targetCamera = pointer.GetComponent<Camera>();
                }
            }
        }

        // Create ring if not assigned
        if (countdownRing == null)
        {
            countdownRing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            countdownRing.name = "CountdownRing";
            countdownRing.transform.SetParent(transform);
            countdownRing.transform.localPosition = Vector3.zero;

            // Remove collider
            Collider col = countdownRing.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            // Setup material
            ringRenderer = countdownRing.GetComponent<Renderer>();
            ringMaterial = new Material(Shader.Find("Unlit/Color"));
            ringMaterial.color = ringStartColor;
            ringRenderer.material = ringMaterial;
        }
        else
        {
            ringRenderer = countdownRing.GetComponent<Renderer>();
            ringMaterial = ringRenderer.material;
        }

        countdownRing.SetActive(false);

        if (showDebugInfo)
        {
            Debug.Log("[GazeHUDCountdown] Initialized");
        }
    }

    /// <summary>
    /// Start countdown with specified duration
    /// </summary>
    public void StartCountdown(float duration)
    {
        countdownDuration = duration;
        currentProgress = 0f;
        isCountingDown = true;

        ShowCountdown();

        if (showDebugInfo)
        {
            Debug.Log($"[GazeHUDCountdown] Started countdown ({duration}s)");
        }
    }

    /// <summary>
    /// Update countdown progress (0-1)
    /// </summary>
    public void UpdateCountdown(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdateRingVisual();

        // Trigger pulse on completion
        if (currentProgress >= 1f && !isPulsing && pulseOnComplete)
        {
            StartPulseAnimation();
        }
    }

    /// <summary>
    /// Update ring visual based on progress
    /// </summary>
    private void UpdateRingVisual()
    {
        if (countdownRing == null || ringMaterial == null) return;

        // Scale based on progress
        float curvedProgress = scaleCurve.Evaluate(currentProgress);
        float scale = Mathf.Lerp(ringMinScale, ringMaxScale, curvedProgress);
        countdownRing.transform.localScale = Vector3.one * scale;

        // Color based on progress
        Color color = Color.Lerp(ringStartColor, ringCompleteColor, currentProgress);
        ringMaterial.color = color;
    }

    /// <summary>
    /// Update ring position to follow reticle
    /// </summary>
    private void UpdateRingPosition()
    {
        if (targetCamera == null || countdownRing == null) return;

        Vector3 targetPosition = targetCamera.transform.position + 
                                targetCamera.transform.forward * distanceFromCamera;
        
        countdownRing.transform.position = targetPosition;
        countdownRing.transform.rotation = targetCamera.transform.rotation;
    }

    /// <summary>
    /// Rotate ring for visual interest
    /// </summary>
    private void UpdateRingRotation()
    {
        if (countdownRing == null) return;

        countdownRing.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Reset countdown to initial state
    /// </summary>
    public void ResetCountdown()
    {
        isCountingDown = false;
        currentProgress = 0f;
        isPulsing = false;

        HideCountdown();

        if (showDebugInfo)
        {
            Debug.Log("[GazeHUDCountdown] Reset");
        }
    }

    /// <summary>
    /// Show countdown visual
    /// </summary>
    private void ShowCountdown()
    {
        if (countdownRing != null)
        {
            countdownRing.SetActive(true);
            countdownRing.transform.localScale = Vector3.one * ringMinScale;
            
            if (ringMaterial != null)
            {
                ringMaterial.color = ringStartColor;
            }
        }
    }

    /// <summary>
    /// Hide countdown visual
    /// </summary>
    private void HideCountdown()
    {
        if (countdownRing != null)
        {
            countdownRing.SetActive(false);
        }
    }

    /// <summary>
    /// Start pulse animation on completion
    /// </summary>
    private void StartPulseAnimation()
    {
        isPulsing = true;
        pulseTimer = 0f;
    }

    /// <summary>
    /// Update pulse animation
    /// </summary>
    private void UpdatePulseAnimation()
    {
        pulseTimer += Time.deltaTime;
        float progress = pulseTimer / pulseDuration;

        if (progress >= 1f)
        {
            isPulsing = false;
            return;
        }

        // Ping-pong scale
        float pulseProgress = Mathf.PingPong(progress * 2f, 1f);
        float scale = Mathf.Lerp(1f, pulseScale, pulseProgress);

        if (countdownRing != null)
        {
            countdownRing.transform.localScale = Vector3.one * ringMaxScale * scale;
        }
    }

    /// <summary>
    /// LEGACY: Backwards compatibility with old SetCountdown signature
    /// </summary>
    public void SetCountdown(float duration, float currentTime)
    {
        if (!isCountingDown)
        {
            StartCountdown(duration);
        }

        float progress = Mathf.Clamp01(currentTime / duration);
        UpdateCountdown(progress);
    }

    /// <summary>
    /// Get current progress
    /// </summary>
    public float GetProgress()
    {
        return currentProgress;
    }

    /// <summary>
    /// Check if counting down
    /// </summary>
    public bool IsCountingDown()
    {
        return isCountingDown;
    }

    /// <summary>
    /// Set ring scale range
    /// </summary>
    public void SetRingScaleRange(float minScale, float maxScale)
    {
        ringMinScale = minScale;
        ringMaxScale = maxScale;
    }

    /// <summary>
    /// Set countdown colors
    /// </summary>
    public void SetColors(Color startColor, Color completeColor)
    {
        ringStartColor = startColor;
        ringCompleteColor = completeColor;
    }

    /// <summary>
    /// Set target camera
    /// </summary>
    public void SetCamera(Camera camera)
    {
        targetCamera = camera;
    }
}
