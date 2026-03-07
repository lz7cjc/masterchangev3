using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GazeReticlePointer - Dot when idle, Ring (torus) when hovering.
/// Reticle maintains constant apparent size on screen via distance-compensated world scaling.
/// 
/// v1.7.0 - UPDATED: Mode-aware raycasting
/// - VR Mode: Gaze-based interactions with reticle pointer
/// - 360 Mode: Touch-based interactions, raycasting disabled
/// </summary>
public class GazeReticlePointer : MonoBehaviour
{
    [Header("Script Info")]
    [SerializeField, Tooltip("Inspector-visible script version (read-only).")]
    private string scriptVersion = "GazeReticlePointer v1.7.0";

    public enum ViewMode { ModeVR, Mode360 }

    [Header("Mode")]
    public ViewMode currentMode = ViewMode.Mode360;

    [Header("Camera Settings")]
    [SerializeField] private Camera attachedCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Reticle Visual Settings")]
    [SerializeField] private GameObject reticleDot;
    [SerializeField] private GameObject reticleRing;
    [SerializeField] private Material reticleMaterial;
    [SerializeField] private float reticleDistance = 2f;

    [Header("Reticle Size")]
    [Tooltip("Outer radius used for both the idle and hover reticle visuals. The script scales the reticle in world-space so it remains the same apparent size on screen regardless of distance.")]
    [SerializeField] private float reticleOuterRadius = 0.015f;

    [Tooltip("How much of the reticle's center is hollow (1 = almost solid, 100 = almost fully hollow). Higher values yield a thinner ring.")]
    [Range(1f, 100f)]
    [SerializeField] private float hollowPercent = 80f;

    // Legacy fields retained for backwards-compatibility with existing prefabs/scenes.
    // These are no longer used by the runtime logic and are hidden to avoid Inspector confusion.
    [HideInInspector, SerializeField] private float dotScale = 0.008f;
    [HideInInspector, SerializeField] private float ringIdleRadius = 0.015f;
    [HideInInspector, SerializeField] private float ringHoverRadius = 0.03f;
    [HideInInspector, SerializeField] private float ringThickness = 0.004f;

    [SerializeField] private int ringSegments = 32;     // Quality of ring (higher = smoother)
    [SerializeField] private float ringScaleSpeed = 8f; // Also used as visual lerp speed

    [Header("Reticle Colors")]
    [SerializeField] private Color dotIdleColor = Color.white;
    [SerializeField] private Color dotHoverColor = Color.cyan;
    [SerializeField] private Color ringIdleColor = new Color(0.2f, 0.3f, 0.6f, 0.8f);
    [SerializeField] private Color ringHoverColor = new Color(0.3f, 0.9f, 0.3f, 1f);

    [Header("Raycast Settings")]
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private bool showDebugRay = false;

    [Header("Mouse Control")]
    [SerializeField] private bool enableMouseControl = true;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool requireRightClick = true;

    [Header("Touch Control")]
    [SerializeField] private bool enableTouchControl = true;
    [SerializeField] private float touchSensitivity = 0.5f;

    [Header("Gyro Control")]
    [SerializeField] private bool enableGyroControl = true;
    [SerializeField] private bool autoEnableGyro = true;

    [Header("Rotation Limits")]
    [SerializeField] private bool limitVerticalRotation = true;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Smoothing")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float smoothingSpeed = 10f;

    [Header("Reticle Display Mode")]
    [SerializeField] private bool showBothDotAndRing = false;

    [Header("NEW - Mode Display Settings")]
    [SerializeField] private bool hideReticleIn360Mode = false;
    [Tooltip("If true, reticle visual is hidden in 360 mode (raycasting already disabled)")]

    // Visual sizing behavior: idle reticle is a solid dot slightly smaller than the hover ring.
    private const float IdleDotScaleFactor = 0.65f;
    private float currentSizeFactor;

    private GameObject currentHoverTarget;
    private GazeHoverTrigger currentHoverTrigger;

    private Material dotMaterialInstance;
    private Material ringMaterialInstance;

    private float currentRingRadius;
    private bool isHovering = false;
    private float currentReticleDistance;

    private float lastReticleOuterRadius;
    private float lastHollowPercent;
    private int lastRingSegments;

    private Vector2 currentRotation;
    private Vector2 targetRotation;
    private bool rightMouseHeld = false;
    private Vector2 lastMousePosition;
    private bool isTouching = false;
    private Vector2 lastTouchPosition;

    void Awake()
    {
        if (autoFindCamera && attachedCamera == null)
        {
            attachedCamera = GetComponent<Camera>();
            if (attachedCamera == null) attachedCamera = Camera.main;
        }

        if (attachedCamera == null)
        {
            Debug.LogError("[GazeReticlePointer] No camera found!");
            enabled = false;
            return;
        }

        if (reticleMaterial == null)
        {
            Debug.LogError("[GazeReticlePointer] No material assigned! Please assign a URP material in Inspector.");
            enabled = false;
            return;
        }

        CreateReticleVisuals();

        CacheReticleShapeSettings();
        RebuildReticleMeshesIfNeeded(force: true);

        Vector3 currentEuler = attachedCamera.transform.localEulerAngles;
        currentRotation = new Vector2(currentEuler.y, currentEuler.x);
        targetRotation = currentRotation;

        if (currentMode == ViewMode.ModeVR && enableGyroControl && autoEnableGyro)
        {
            EnableGyroscope();
        }

        currentRingRadius = reticleOuterRadius;
        currentReticleDistance = reticleDistance;
        currentSizeFactor = IdleDotScaleFactor;
    }

    void OnValidate()
    {
        // Keep version pinned for quick visual verification in Inspector (including during Play Mode).
        scriptVersion = "GazeReticlePointer v1.7.0";

        if (reticleOuterRadius < 0.0001f) reticleOuterRadius = 0.0001f;
        hollowPercent = Mathf.Clamp(hollowPercent, 1f, 100f);
        if (ringSegments < 3) ringSegments = 3;

        // Rebuild in-editor immediately when tuning values.
        RebuildReticleMeshesIfNeeded(force: false);
    }

    void Update()
    {
        HandleInput();
        ApplyRotation();
        PerformGazeRaycast();

        // Support real-time tuning while playing (Inspector changes to radius/hollow/segments).
        RebuildReticleMeshesIfNeeded(force: false);

        UpdateReticleVisuals();
    }

    private void HandleInput()
    {
        if (currentMode == ViewMode.ModeVR)
        {
            if (Application.isMobilePlatform && enableGyroControl && Input.gyro.enabled)
            {
                HandleGyroInput();
            }
            else if (enableMouseControl)
            {
                HandleMouseInput();
            }
        }
        else if (currentMode == ViewMode.Mode360)
        {
            if (Application.isMobilePlatform && enableTouchControl)
            {
                HandleTouchInput();
            }
            else if (enableMouseControl)
            {
                HandleMouseInput();
            }
        }
    }

    private void HandleMouseInput()
    {
        // In VR mode: Use RIGHT-click (for head movement simulation)
        // In 360 mode: Use LEFT-click (for camera drag - more intuitive)

        bool isVRMode = (currentMode == ViewMode.ModeVR);
        bool useRightClick = isVRMode; // VR uses right-click, 360 uses left-click

        bool mousePressed = false;

        if (Mouse.current != null)
        {
            if (useRightClick)
            {
                // VR Mode: RIGHT-click for head movement
                mousePressed = Mouse.current.rightButton.isPressed;
            }
            else
            {
                // 360 Mode: LEFT-click for camera drag
                mousePressed = Mouse.current.leftButton.isPressed;
            }
        }

        if (mousePressed)
        {
            if (!rightMouseHeld)
            {
                rightMouseHeld = true;
                if (Mouse.current != null)
                {
                    lastMousePosition = Mouse.current.position.ReadValue();
                }
            }

            if (Mouse.current != null)
            {
                Vector2 currentMousePos = Mouse.current.position.ReadValue();
                Vector2 mouseDelta = currentMousePos - lastMousePosition;
                lastMousePosition = currentMousePos;

                targetRotation.x += mouseDelta.x * mouseSensitivity * 0.1f;
                targetRotation.y -= mouseDelta.y * mouseSensitivity * 0.1f;

                if (limitVerticalRotation)
                {
                    targetRotation.y = Mathf.Clamp(targetRotation.y, minVerticalAngle, maxVerticalAngle);
                }
            }
        }
        else
        {
            rightMouseHeld = false;
        }
    }

    private void HandleTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        // Get primary touch (first active touch)
        var touches = touchscreen.touches;
        if (touches.Count == 0)
        {
            isTouching = false;
            return;
        }

        var touch = touches[0];

        // Check if touch is active
        if (!touch.isInProgress)
        {
            isTouching = false;
            return;
        }

        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            isTouching = true;
            lastTouchPosition = touch.position.ReadValue();
        }
        else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved && isTouching)
        {
            Vector2 currentTouchPos = touch.position.ReadValue();
            Vector2 touchDelta = currentTouchPos - lastTouchPosition;
            lastTouchPosition = currentTouchPos;

            targetRotation.x += touchDelta.x * touchSensitivity * 0.1f;
            targetRotation.y -= touchDelta.y * touchSensitivity * 0.1f;

            if (limitVerticalRotation)
            {
                targetRotation.y = Mathf.Clamp(targetRotation.y, minVerticalAngle, maxVerticalAngle);
            }
        }
        else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            isTouching = false;
        }
    }

    private void HandleGyroInput()
    {
        if (!Input.gyro.enabled) return;

        Quaternion gyroRotation = Input.gyro.attitude;
        gyroRotation = new Quaternion(gyroRotation.x, gyroRotation.y, -gyroRotation.z, -gyroRotation.w);
        attachedCamera.transform.localRotation = gyroRotation * Quaternion.Euler(90f, 0f, 0f);
    }

    private void ApplyRotation()
    {
        if (currentMode == ViewMode.ModeVR && Application.isMobilePlatform &&
            enableGyroControl && Input.gyro.enabled)
        {
            return;
        }

        if (useSmoothing)
        {
            currentRotation = Vector2.Lerp(currentRotation, targetRotation, Time.deltaTime * smoothingSpeed);
        }
        else
        {
            currentRotation = targetRotation;
        }

        attachedCamera.transform.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
    }

    /// <summary>
    /// UPDATED v1.7.1: Raycasting enabled in both modes.
    /// VR Mode:  gaze-based (gyro/mouse right-click rotates camera → reticle hits orb)
    /// 360 Mode: finger-drag rotates camera → same reticle raycast fires → orbs respond.
    ///           Tap/click also works because camera is already aimed at the orb.
    /// </summary>
    private void PerformGazeRaycast()
    {
        // Both modes raycast — input method differs but orb interaction is identical.
        Ray ray = new Ray(attachedCamera.transform.position, attachedCamera.transform.forward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.green);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, interactableLayers))
        {
            // Pull reticle 0.05 units in front of the hit surface so it never clips inside the orb
            currentReticleDistance = Mathf.Max(hit.distance - 0.05f, 0.1f);

            GameObject hitObject = hit.collider.gameObject;
            GazeHoverTrigger hitTrigger = hitObject.GetComponent<GazeHoverTrigger>();

            if (hitTrigger != null)
            {
                if (currentHoverTarget != hitObject)
                {
                    ExitCurrentTarget();
                    EnterNewTarget(hitObject, hitTrigger);
                }
                isHovering = true;
            }
            else
            {
                if (currentHoverTarget != null)
                {
                    ExitCurrentTarget();
                }
                isHovering = false;
            }
        }
        else
        {
            currentReticleDistance = reticleDistance;

            if (currentHoverTarget != null)
            {
                ExitCurrentTarget();
            }
            isHovering = false;
        }
    }

    private void EnterNewTarget(GameObject target, GazeHoverTrigger trigger)
    {
        currentHoverTarget = target;
        currentHoverTrigger = trigger;
        trigger.OnGazeEnter();
    }

    private void ExitCurrentTarget()
    {
        if (currentHoverTrigger != null)
        {
            currentHoverTrigger.OnGazeExit();
        }
        currentHoverTarget = null;
        currentHoverTrigger = null;
    }

    private void UpdateReticleVisuals()
    {
        // NEW: Optionally hide reticle in 360 mode
        if (hideReticleIn360Mode && currentMode == ViewMode.Mode360)
        {
            if (reticleDot != null) reticleDot.SetActive(false);
            if (reticleRing != null) reticleRing.SetActive(false);
            return;
        }

        // Toggle logic
        if (showBothDotAndRing)
        {
            if (reticleDot != null) reticleDot.SetActive(true);
            if (reticleRing != null) reticleRing.SetActive(true);
        }
        else
        {
            if (isHovering)
            {
                if (reticleDot != null) reticleDot.SetActive(false);
                if (reticleRing != null) reticleRing.SetActive(true);
            }
            else
            {
                if (reticleDot != null) reticleDot.SetActive(true);
                if (reticleRing != null) reticleRing.SetActive(false);
            }
        }

        UpdateReticlePosition();
        UpdateReticleSize();
        UpdateReticleColors();
    }

    private void UpdateReticlePosition()
    {
        Vector3 targetPosition = attachedCamera.transform.position +
                                 attachedCamera.transform.forward * currentReticleDistance;

        if (reticleDot != null)
        {
            reticleDot.transform.position = targetPosition;
            reticleDot.transform.rotation = Quaternion.LookRotation(
                attachedCamera.transform.forward, attachedCamera.transform.up);
        }

        if (reticleRing != null)
        {
            reticleRing.transform.position = targetPosition;
            reticleRing.transform.rotation = Quaternion.LookRotation(
                attachedCamera.transform.forward, attachedCamera.transform.up);
        }
    }

    private void UpdateReticleSize()
    {
        float targetSize = isHovering ? 1f : IdleDotScaleFactor;
        currentSizeFactor = Mathf.Lerp(currentSizeFactor, targetSize, Time.deltaTime * ringScaleSpeed);

        float worldScale = currentSizeFactor * reticleOuterRadius * currentReticleDistance;

        if (reticleDot != null && reticleDot.activeSelf)
        {
            SetWorldUniformScale(reticleDot.transform, worldScale);
        }

        if (reticleRing != null && reticleRing.activeSelf)
        {
            SetWorldUniformScale(reticleRing.transform, worldScale);
        }
    }

    private void UpdateReticleColors()
    {
        float speed = ringScaleSpeed;

        if (isHovering)
        {
            if (dotMaterialInstance != null)
            {
                LerpMaterialColor(dotMaterialInstance, dotHoverColor, speed);
            }
            if (ringMaterialInstance != null)
            {
                LerpMaterialColor(ringMaterialInstance, ringHoverColor, speed);
            }
        }
        else
        {
            if (dotMaterialInstance != null)
            {
                LerpMaterialColor(dotMaterialInstance, dotIdleColor, speed);
            }
            if (ringMaterialInstance != null)
            {
                LerpMaterialColor(ringMaterialInstance, ringIdleColor, speed);
            }
        }
    }

    private void LerpMaterialColor(Material mat, Color targetColor, float speed)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), targetColor, Time.deltaTime * speed));
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.Lerp(mat.GetColor("_Color"), targetColor, Time.deltaTime * speed));
        }
    }

    /// <summary>
    /// Sets a uniform scale in world-space even when the transform is parented under a scaled hierarchy.
    /// For a desired world uniform scale S, localScale must be S / parent.lossyScale (per axis).
    /// </summary>
    private static void SetWorldUniformScale(Transform t, float desiredWorldUniformScale)
    {
        Transform p = t.parent;
        if (p == null)
        {
            t.localScale = Vector3.one * desiredWorldUniformScale;
            return;
        }

        Vector3 parentLossy = p.lossyScale;
        float sx = Mathf.Abs(parentLossy.x) > 0.000001f ? desiredWorldUniformScale / parentLossy.x : desiredWorldUniformScale;
        float sy = Mathf.Abs(parentLossy.y) > 0.000001f ? desiredWorldUniformScale / parentLossy.y : desiredWorldUniformScale;
        float sz = Mathf.Abs(parentLossy.z) > 0.000001f ? desiredWorldUniformScale / parentLossy.z : desiredWorldUniformScale;

        t.localScale = new Vector3(sx, sy, sz);
    }

    private void CreateReticleVisuals()
    {
        // Create DOT (solid sphere) if not provided.
        if (reticleDot == null)
        {
            reticleDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleDot.name = "ReticleDot";
            reticleDot.transform.SetParent(transform);

            Collider c = reticleDot.GetComponent<Collider>();
            if (c != null) Destroy(c);

            dotMaterialInstance = new Material(reticleMaterial);
            ApplyMaterialColor(dotMaterialInstance, dotIdleColor);

            Renderer r = reticleDot.GetComponent<Renderer>();
            if (r != null) r.material = dotMaterialInstance;

            reticleDot.SetActive(showBothDotAndRing);
        }
        else
        {
            var r = reticleDot.GetComponent<Renderer>();
            if (r != null)
            {
                dotMaterialInstance = r.material != null ? r.material : new Material(reticleMaterial);
                ApplyMaterialColor(dotMaterialInstance, dotIdleColor);
                r.material = dotMaterialInstance;
            }
        }

        // Create RING (torus) if not provided.
        if (reticleRing == null)
        {
            reticleRing = new GameObject("ReticleRing");
            reticleRing.transform.SetParent(transform);

            MeshFilter meshFilter = reticleRing.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateReticleTorusMesh();

            MeshRenderer meshRenderer = reticleRing.AddComponent<MeshRenderer>();
            ringMaterialInstance = new Material(reticleMaterial);
            ApplyMaterialColor(ringMaterialInstance, ringIdleColor);
            meshRenderer.material = ringMaterialInstance;

            reticleRing.SetActive(false);
        }
        else
        {
            var mr = reticleRing.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                ringMaterialInstance = mr.material != null ? mr.material : new Material(reticleMaterial);
                ApplyMaterialColor(ringMaterialInstance, ringIdleColor);
                mr.material = ringMaterialInstance;
            }
        }
    }

    private static void ApplyMaterialColor(Material mat, Color c)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }

    private void CacheReticleShapeSettings()
    {
        lastReticleOuterRadius = reticleOuterRadius;
        lastHollowPercent = hollowPercent;
        lastRingSegments = ringSegments;
    }

    private void RebuildReticleMeshesIfNeeded(bool force)
    {
        if (!force &&
            Mathf.Approximately(lastReticleOuterRadius, reticleOuterRadius) &&
            Mathf.Approximately(lastHollowPercent, hollowPercent) &&
            lastRingSegments == ringSegments)
        {
            return;
        }

        CacheReticleShapeSettings();

        if (reticleRing != null)
        {
            MeshFilter ringFilter = reticleRing.GetComponent<MeshFilter>();
            if (ringFilter != null) ringFilter.sharedMesh = CreateReticleTorusMesh();
        }
    }

    private Mesh CreateReticleTorusMesh()
    {
        float outer = Mathf.Max(reticleOuterRadius, 0.0001f);

        // hollowPercent ~= innerRadius / outerRadius (as a percentage).
        float hollowRatio = Mathf.Clamp(hollowPercent / 100f, 0.01f, 0.99f);
        float inner = outer * hollowRatio;

        float radius = (outer + inner) * 0.5f;
        float thickness = (outer - inner) * 0.5f;

        return CreateTorusMesh(radius, thickness, ringSegments, 16);
    }

    // Creates a torus (donut) mesh
    private Mesh CreateTorusMesh(float radius, float thickness, int radialSegments, int tubularSegments)
    {
        Mesh mesh = new Mesh();

        int vertexCount = (radialSegments + 1) * (tubularSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        int idx = 0;
        for (int i = 0; i <= radialSegments; i++)
        {
            float u = (float)i / radialSegments * 2f * Mathf.PI;
            Vector3 circleCenter = new Vector3(Mathf.Cos(u) * radius, Mathf.Sin(u) * radius, 0f);

            for (int j = 0; j <= tubularSegments; j++)
            {
                float v = (float)j / tubularSegments * 2f * Mathf.PI;
                float x = (radius + thickness * Mathf.Cos(v)) * Mathf.Cos(u);
                float y = (radius + thickness * Mathf.Cos(v)) * Mathf.Sin(u);
                float z = thickness * Mathf.Sin(v);

                vertices[idx] = new Vector3(x, y, z);

                Vector3 normal = (vertices[idx] - circleCenter).normalized;
                normals[idx] = normal;

                uvs[idx] = new Vector2((float)i / radialSegments, (float)j / tubularSegments);

                idx++;
            }
        }

        int triangleCount = radialSegments * tubularSegments * 6;
        int[] triangles = new int[triangleCount];

        int triIdx = 0;
        for (int i = 0; i < radialSegments; i++)
        {
            for (int j = 0; j < tubularSegments; j++)
            {
                int a = i * (tubularSegments + 1) + j;
                int b = (i + 1) * (tubularSegments + 1) + j;
                int c = (i + 1) * (tubularSegments + 1) + (j + 1);
                int d = i * (tubularSegments + 1) + (j + 1);

                triangles[triIdx++] = a;
                triangles[triIdx++] = b;
                triangles[triIdx++] = c;

                triangles[triIdx++] = a;
                triangles[triIdx++] = c;
                triangles[triIdx++] = d;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        return mesh;
    }

    private void EnableGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("[GazeReticlePointer] Gyroscope enabled");
        }
    }

    public void SetMode(ViewMode mode)
    {
        currentMode = mode;
        if (mode == ViewMode.ModeVR && enableGyroControl && autoEnableGyro && Application.isMobilePlatform)
        {
            EnableGyroscope();
        }

        Debug.Log($"[GazeReticlePointer] Mode set to: {mode}");
    }

    public GameObject GetCurrentHoverTarget() => currentHoverTarget;
    public bool IsHoveringInteractive() => isHovering;
}