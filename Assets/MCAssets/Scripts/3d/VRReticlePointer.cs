using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class VRReticlePointer : MonoBehaviour
{
    public enum ViewMode { Mode2D, Mode360, ModeVR }

    [Header("Mode Settings")]
    [SerializeField] private ViewMode currentMode = ViewMode.Mode360;
    [SerializeField] private bool showDebugRay = false;

    [Header("Camera Movement Settings")]
    [SerializeField, Range(0.1f, 20f)] private float horizontalSensitivity = 2f;
    [SerializeField, Range(0.1f, 20f)] private float verticalSensitivity = 2f;
    [SerializeField] private float dampingStrength = 5f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Movement Limits")]
    [SerializeField] private bool limitVerticalRotation = true;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Reticle Settings")]
    [SerializeField] private float maxReticleDistance = 10f;
    [SerializeField] private Image reticleImage;
    [SerializeField] private float reticleSmoothSpeed = 10f;
    [SerializeField] private bool smoothReticleMovement = true;
    [SerializeField] private LayerMask interactableLayers = -1;

    [Header("Dot Settings")]
    [SerializeField, Range(0.01f, 0.5f)] private float dotSize = 0.02f;
    [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Circle Settings")]
    [SerializeField, Range(0.01f, 0.5f)] private float circleSize = 0.05f;
    [SerializeField, Range(1f, 10f)] private float circleThickness = 2f;
    [SerializeField] private Color circleColor = new Color(0f, 1f, 0f, 0.8f);

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction touchPressAction;
    private InputAction clickAction;
    private Camera mainCamera;
    private GameObject currentTarget;
    private bool isCircle = false;
    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Vector3 currentReticlePosition;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();

        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found!");
            return;
        }

        lookAction = playerInput.actions["Look"];
        touchPressAction = playerInput.actions["TouchPress"];
        clickAction = playerInput.actions["Click"];

        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;

        if (reticleImage == null) CreateReticle();
        ConfigureControls();
    }

    private void ConfigureControls()
    {
        touchPressAction.performed -= StartRotation;
        touchPressAction.canceled -= StopRotation;
        clickAction.performed -= HandleClick;

#if UNITY_EDITOR
        if (currentMode == ViewMode.Mode360)
        {
            touchPressAction.performed += StartRotation;
            touchPressAction.canceled += StopRotation;
        }
        if (currentMode == ViewMode.Mode2D)
        {
            clickAction.performed += HandleClick;
        }
#else
        if (currentMode == ViewMode.Mode360)
        {
            touchPressAction.performed += StartRotation;
            touchPressAction.canceled += StopRotation;
        }
        if (currentMode == ViewMode.Mode2D)
        {
            clickAction.performed += HandleClick;
        }
#endif
    }

    private void OnEnable()
    {
        ConfigureControls();
    }

    private void OnDisable()
    {
        touchPressAction.performed -= StartRotation;
        touchPressAction.canceled -= StopRotation;
        clickAction.performed -= HandleClick;
    }

    private void LateUpdate()
    {
        if (currentMode == ViewMode.ModeVR) return;

        if (currentMode == ViewMode.Mode360 && isRotating)
        {
            HandleRotation();
        }

        UpdateReticle();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        Vector2 lookDelta = lookInput - previousLookInput;

        // Allow full 360 horizontal rotation
        targetRotation.y += lookDelta.x * horizontalSensitivity;
        
        // Limit vertical rotation to -90/90
        targetRotation.x = Mathf.Clamp(targetRotation.x - lookDelta.y * verticalSensitivity, -90f, 90f);

        if (useSmoothing)
        {
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * dampingStrength);
            transform.rotation = Quaternion.Euler(currentRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(targetRotation);
        }

        previousLookInput = lookInput;
    }

    private void StartRotation(InputAction.CallbackContext context)
    {
        if (currentMode == ViewMode.Mode2D || currentMode == ViewMode.ModeVR) return;

        isRotating = true;
        previousLookInput = lookAction.ReadValue<Vector2>();
        currentRotation = new Vector3(
            Mathf.Clamp(transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x, -90f, 90f),
            transform.eulerAngles.y,
            0
        );
        targetRotation = currentRotation;
    }
    private void StopRotation(InputAction.CallbackContext context)
    {
        isRotating = false;
    }

    private void UpdateReticle()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxReticleDistance, interactableLayers))
        {
            UpdateReticlePosition(hitInfo.point);
            HandleTargetInteraction(hitInfo.collider.gameObject);
        }
        else
        {
            UpdateReticlePosition(ray.GetPoint(maxReticleDistance));
            ClearCurrentTarget();
        }
    }

    private void HandleClick(InputAction.CallbackContext context)
    {
        if (currentMode != ViewMode.Mode2D || currentTarget == null) return;

        EventTrigger eventTrigger = currentTarget.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            var clickEntry = eventTrigger.triggers.Find(
                trigger => trigger.eventID == EventTriggerType.PointerClick);

            if (clickEntry != null)
            {
                var eventData = new PointerEventData(EventSystem.current);
                clickEntry.callback.Invoke(eventData);
            }
        }
    }

    private void HandleTargetInteraction(GameObject hitObject)
    {
        EventTrigger eventTrigger = hitObject.GetComponent<EventTrigger>();

        if (eventTrigger != null)
        {
            if (currentTarget != hitObject)
            {
                ClearCurrentTarget();
                currentTarget = hitObject;
                TriggerPointerEnter(hitObject);
                SetReticleState(true);
            }
        }
        else if (currentTarget != null)
        {
            ClearCurrentTarget();
        }
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            TriggerPointerExit(currentTarget);
            currentTarget = null;
            SetReticleState(false);
        }
    }

    private void TriggerPointerEnter(GameObject target)
    {
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            var enterEntry = eventTrigger.triggers.Find(
                trigger => trigger.eventID == EventTriggerType.PointerEnter);

            if (enterEntry != null)
            {
                var eventData = new PointerEventData(EventSystem.current);
                enterEntry.callback.Invoke(eventData);
            }
        }
    }

    private void TriggerPointerExit(GameObject target)
    {
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            var exitEntry = eventTrigger.triggers.Find(
                trigger => trigger.eventID == EventTriggerType.PointerExit);

            if (exitEntry != null)
            {
                var eventData = new PointerEventData(EventSystem.current);
                exitEntry.callback.Invoke(eventData);
            }
        }
    }

    private void CreateReticle()
    {
        GameObject canvasObj = new GameObject("Reticle Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;  // Ensures reticle renders on top
        canvasObj.transform.SetParent(transform, false);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1f;

        GameObject reticleObj = new GameObject("Reticle");
        reticleObj.transform.SetParent(canvasObj.transform, false);
        reticleImage = reticleObj.AddComponent<Image>();

        // Set initial size
        reticleImage.rectTransform.sizeDelta = Vector2.one * dotSize;

        UpdateReticleAppearance();
    }

    private void UpdateReticlePosition(Vector3 targetPosition)
    {
        if (reticleImage == null) return;

        Vector3 directionToTarget = targetPosition - mainCamera.transform.position;
        // Move reticle very slightly in front of hit point (0.001 units)
        Vector3 adjustedPosition = targetPosition - directionToTarget.normalized * 0.001f;

        if (smoothReticleMovement)
        {
            currentReticlePosition = Vector3.Lerp(currentReticlePosition, adjustedPosition,
                Time.deltaTime * reticleSmoothSpeed);
            reticleImage.transform.position = currentReticlePosition;
        }
        else
        {
            reticleImage.transform.position = adjustedPosition;
            currentReticlePosition = adjustedPosition;
        }

        reticleImage.transform.rotation = Quaternion.LookRotation(-directionToTarget);
    }
    private void SetReticleState(bool active)
    {
        if (isCircle != active)
        {
            isCircle = active;
            UpdateReticleAppearance();
        }
    }

    private Sprite CreateDotSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];

        float center = 32f;
        float radius = 31f;

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                colors[y * 64 + x] = distanceFromCenter <= radius ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];

        float center = 32f;
        float outerRadius = 31f;
        float innerRadius = outerRadius - circleThickness;

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                colors[y * 64 + x] = (distanceFromCenter <= outerRadius && distanceFromCenter >= innerRadius) ?
                    Color.white : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    private void UpdateReticleAppearance()
    {
        if (reticleImage != null)
        {
            reticleImage.sprite = isCircle ? CreateCircleSprite() : CreateDotSprite();
            reticleImage.color = isCircle ? circleColor : dotColor;
            reticleImage.rectTransform.sizeDelta = Vector2.one * (isCircle ? circleSize : dotSize);
        }
    }

    public void SetMode(ViewMode newMode)
    {
        currentMode = newMode;
        ConfigureControls();
    }

    private void OnDrawGizmos()
    {
        if (showDebugRay && mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * maxReticleDistance);
        }
    }
}