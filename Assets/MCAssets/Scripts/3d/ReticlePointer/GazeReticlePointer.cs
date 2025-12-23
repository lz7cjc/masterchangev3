using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GazeReticlePointer - Dot when idle, Ring (torus) when hovering
/// </summary>
public class GazeReticlePointer : MonoBehaviour
{
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
    
    [Header("Dot Settings")]
    [SerializeField] private float dotScale = 0.008f;
    
    [Header("Ring Settings")]
    [SerializeField] private float ringIdleRadius = 0.015f;      // Outer radius when not hovering
    [SerializeField] private float ringHoverRadius = 0.03f;       // Outer radius when hovering
    [SerializeField] private float ringThickness = 0.004f;        // How thick the ring is
    [SerializeField] private int ringSegments = 32;               // Quality of ring (higher = smoother)
    [SerializeField] private float ringScaleSpeed = 8f;

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

    private GameObject currentHoverTarget;
    private GazeHoverTrigger currentHoverTrigger;
    private Renderer reticleDotRenderer;
    private Renderer reticleRingRenderer;
    private Material dotMaterialInstance;
    private Material ringMaterialInstance;
    private float currentRingRadius;
    private bool isHovering = false;
    private float currentReticleDistance;

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

        Vector3 currentEuler = attachedCamera.transform.localEulerAngles;
        currentRotation = new Vector2(currentEuler.y, currentEuler.x);
        targetRotation = currentRotation;

        if (currentMode == ViewMode.ModeVR && enableGyroControl && autoEnableGyro)
        {
            EnableGyroscope();
        }

        currentRingRadius = ringIdleRadius;
        currentReticleDistance = reticleDistance;
    }

    void Update()
    {
        HandleInput();
        ApplyRotation();
        PerformGazeRaycast();
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
        if (requireRightClick)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                if (!rightMouseHeld)
                {
                    rightMouseHeld = true;
                    lastMousePosition = Mouse.current.position.ReadValue();
                }

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
            else
            {
                rightMouseHeld = false;
            }
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;
        
        if (touches[0].isInProgress)
        {
            Vector2 touchPos = touches[0].position.ReadValue();

            if (!isTouching)
            {
                isTouching = true;
                lastTouchPosition = touchPos;
            }
            else
            {
                Vector2 touchDelta = touchPos - lastTouchPosition;
                lastTouchPosition = touchPos;

                targetRotation.x += touchDelta.x * touchSensitivity;
                targetRotation.y -= touchDelta.y * touchSensitivity;

                if (limitVerticalRotation)
                {
                    targetRotation.y = Mathf.Clamp(targetRotation.y, minVerticalAngle, maxVerticalAngle);
                }
            }
        }
        else
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

    private void PerformGazeRaycast()
    {
        Ray ray = new Ray(attachedCamera.transform.position, attachedCamera.transform.forward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.green);
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayers))
        {
            currentReticleDistance = Mathf.Min(hit.distance, reticleDistance);
            
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
        // Toggle logic
        if (showBothDotAndRing)
        {
            if (reticleDot != null) reticleDot.SetActive(true);
            if (reticleRing != null) reticleRing.SetActive(true);
        }
        else
        {
            if (reticleDot != null) reticleDot.SetActive(!isHovering);
            if (reticleRing != null) reticleRing.SetActive(isHovering);
        }

        Vector3 reticlePosition = attachedCamera.transform.position + 
                                 attachedCamera.transform.forward * currentReticleDistance;

        // Update dot
        if (reticleDot != null && reticleDot.activeSelf)
        {
            reticleDot.transform.position = reticlePosition;
            reticleDot.transform.rotation = Quaternion.LookRotation(
                reticlePosition - attachedCamera.transform.position);
            reticleDot.transform.localScale = Vector3.one * dotScale;
            
            if (dotMaterialInstance != null)
            {
                Color targetColor = isHovering ? dotHoverColor : dotIdleColor;
                
                if (dotMaterialInstance.HasProperty("_BaseColor"))
                {
                    dotMaterialInstance.SetColor("_BaseColor", Color.Lerp(
                        dotMaterialInstance.GetColor("_BaseColor"), targetColor, Time.deltaTime * ringScaleSpeed));
                }
                else if (dotMaterialInstance.HasProperty("_Color"))
                {
                    dotMaterialInstance.SetColor("_Color", Color.Lerp(
                        dotMaterialInstance.GetColor("_Color"), targetColor, Time.deltaTime * ringScaleSpeed));
                }
            }
        }

        // Update ring - NOW SCALES THE ACTUAL RING MESH
        if (reticleRing != null && reticleRing.activeSelf)
        {
            reticleRing.transform.position = reticlePosition;
            reticleRing.transform.rotation = Quaternion.LookRotation(
                reticlePosition - attachedCamera.transform.position);
            
            // Animate ring size
            float targetRadius = isHovering ? ringHoverRadius : ringIdleRadius;
            currentRingRadius = Mathf.Lerp(currentRingRadius, targetRadius, Time.deltaTime * ringScaleSpeed);
            
            // Scale the ring (torus maintains its proportions)
            float scaleRatio = currentRingRadius / ringIdleRadius;
            reticleRing.transform.localScale = Vector3.one * scaleRatio;
            
            if (ringMaterialInstance != null)
            {
                Color targetColor = isHovering ? ringHoverColor : ringIdleColor;
                
                if (ringMaterialInstance.HasProperty("_BaseColor"))
                {
                    ringMaterialInstance.SetColor("_BaseColor", Color.Lerp(
                        ringMaterialInstance.GetColor("_BaseColor"), targetColor, Time.deltaTime * ringScaleSpeed));
                }
                else if (ringMaterialInstance.HasProperty("_Color"))
                {
                    ringMaterialInstance.SetColor("_Color", Color.Lerp(
                        ringMaterialInstance.GetColor("_Color"), targetColor, Time.deltaTime * ringScaleSpeed));
                }
            }
        }
    }

    private void CreateReticleVisuals()
    {
        // Create DOT (sphere)
        if (reticleDot == null)
        {
            reticleDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleDot.name = "ReticleDot";
            reticleDot.transform.SetParent(transform);
            Destroy(reticleDot.GetComponent<Collider>());
            
            reticleDotRenderer = reticleDot.GetComponent<Renderer>();
            dotMaterialInstance = new Material(reticleMaterial);
            
            if (dotMaterialInstance.HasProperty("_BaseColor"))
            {
                dotMaterialInstance.SetColor("_BaseColor", dotIdleColor);
            }
            else if (dotMaterialInstance.HasProperty("_Color"))
            {
                dotMaterialInstance.SetColor("_Color", dotIdleColor);
            }
            
            reticleDotRenderer.material = dotMaterialInstance;
            reticleDot.SetActive(showBothDotAndRing);
        }

        // Create RING (torus)
        if (reticleRing == null)
        {
            reticleRing = new GameObject("ReticleRing");
            reticleRing.transform.SetParent(transform);
            
            // Create torus mesh
            MeshFilter meshFilter = reticleRing.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateTorusMesh(ringIdleRadius, ringThickness, ringSegments, 16);
            
            // Add renderer
            reticleRingRenderer = reticleRing.AddComponent<MeshRenderer>();
            ringMaterialInstance = new Material(reticleMaterial);
            
            if (ringMaterialInstance.HasProperty("_BaseColor"))
            {
                ringMaterialInstance.SetColor("_BaseColor", ringIdleColor);
            }
            else if (ringMaterialInstance.HasProperty("_Color"))
            {
                ringMaterialInstance.SetColor("_Color", ringIdleColor);
            }
            
            reticleRingRenderer.material = ringMaterialInstance;
            reticleRing.SetActive(false);
        }
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
        
        // Create triangles
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
    }

    public GameObject GetCurrentHoverTarget() => currentHoverTarget;
    public bool IsHoveringInteractive() => isHovering;
}
