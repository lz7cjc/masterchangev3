using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// UPDATED: Now uses UIReticlePointer for screen-space reticle that's always visible
/// Handles raycasting and interaction logic for VR and 360 modes
/// </summary>
public class VRReticlePointer : MonoBehaviour
{
    public enum ViewMode { Mode2D, Mode360, ModeVR }

    [Header("Mode Settings")]
    [SerializeField] private ViewMode currentMode = ViewMode.Mode360;
    [SerializeField] private bool showDebugRay = false;

    [Header("Camera Movement Settings")]
    [SerializeField, Range(0.1f, 20f)] private float horizontalSensitivity = 0.5f;
    [SerializeField, Range(0.1f, 20f)] private float verticalSensitivity = 0.5f;
    [SerializeField] private float dampingStrength = 5f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Movement Limits")]
    [SerializeField] private bool limitVerticalRotation = true;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Interaction Settings")]
    [SerializeField] private float maxInteractionDistance = 10f;
    [SerializeField] private LayerMask interactableLayers = -1;

    [Header("Reticle Settings")]
    [SerializeField] private bool createReticleAutomatically = true;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> OnPointerEnter;
    [SerializeField] private UnityEvent<GameObject> OnPointerExit;

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction touchPressAction;
    private InputAction clickAction;

    private Camera mainCamera;
    private GameObject currentTarget;

    private UIReticlePointer reticle; // NEW: UI-based reticle

    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    // Flag to track if we're using Input System or fallback
    private bool useInputSystemFallback = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();

        // NEW: Setup UI reticle system
        SetupReticle();

        // Try to get Input Actions, but don't fail if they're not configured
        if (playerInput != null)
        {
            try
            {
                lookAction = playerInput.actions["Look"];
                touchPressAction = playerInput.actions["TouchPress"];
                clickAction = playerInput.actions["Click"];

                Debug.Log("[VRReticlePointer] Using Input System actions");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VRReticlePointer] Input Actions not configured: {e.Message}");
                Debug.LogWarning("[VRReticlePointer] Falling back to direct mouse input for editor testing");
                useInputSystemFallback = true;
                playerInput = null;
            }
        }
        else
        {
            Debug.LogWarning("[VRReticlePointer] No PlayerInput component - using mouse input fallback");
            useInputSystemFallback = true;
        }

        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;

        ConfigureControls();
    }

    private void Start()
    {
        // FIXED: Ensure reticle starts in idle state (not hovering)
        if (reticle != null)
        {
            reticle.SetHoverState(false);
            reticle.SetActiveState(false);

            Debug.Log("[VRReticlePointer] Reticle initialized to idle state");
        }
    }

    /// <summary>
    /// NEW: Setup the UI-based reticle
    /// </summary>
    private void SetupReticle()
    {
        // Check if reticle already exists
        reticle = GetComponent<UIReticlePointer>();

        if (reticle == null && createReticleAutomatically)
        {
            // Create the reticle component
            reticle = gameObject.AddComponent<UIReticlePointer>();
            Debug.Log("[VRReticlePointer] UIReticlePointer created automatically");
        }
        else if (reticle != null)
        {
            Debug.Log("[VRReticlePointer] Using existing UIReticlePointer");
        }
        else
        {
            Debug.LogWarning("[VRReticlePointer] No reticle will be displayed - add UIReticlePointer manually if needed");
        }
    }

    private void ConfigureControls()
    {
        // Only configure if we have valid Input System setup
        if (playerInput == null || useInputSystemFallback)
            return;

        if (touchPressAction != null)
        {
            touchPressAction.performed -= StartRotation;
            touchPressAction.canceled -= StopRotation;
        }
        if (clickAction != null)
        {
            clickAction.performed -= HandleClick;
        }

        if (currentMode == ViewMode.Mode360)
        {
            if (touchPressAction != null)
            {
                touchPressAction.performed += StartRotation;
                touchPressAction.canceled += StopRotation;
            }
        }
        if (currentMode == ViewMode.Mode2D)
        {
            if (clickAction != null)
            {
                clickAction.performed += HandleClick;
            }
        }
    }

    private void OnEnable()
    {
        ConfigureControls();
    }

    private void OnDisable()
    {
        if (touchPressAction != null)
        {
            touchPressAction.performed -= StartRotation;
            touchPressAction.canceled -= StopRotation;
        }
        if (clickAction != null)
        {
            clickAction.performed -= HandleClick;
        }
    }

    private void LateUpdate()
    {
        if (currentMode == ViewMode.ModeVR) return;

        // Handle rotation in 360 mode
        if (currentMode == ViewMode.Mode360)
        {
            // If using fallback input, handle mouse directly
            if (useInputSystemFallback || playerInput == null)
            {
                HandleMouseInput();
            }

            if (isRotating)
            {
                HandleRotation();
            }
        }

        CheckInteractions();
    }

    private void HandleMouseInput()
    {
        // Direct mouse input for editor testing when Input System is not configured
        // Use right mouse button for rotation
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            isRotating = true;
            previousLookInput = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            currentRotation = new Vector3(
                Mathf.Clamp(transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x, minVerticalAngle, maxVerticalAngle),
                transform.eulerAngles.y,
                0
            );
            targetRotation = currentRotation;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }
    }

    private void HandleRotation()
    {
        Vector2 lookInput;

        // Use Input System if configured, otherwise use direct mouse input
        if (!useInputSystemFallback && playerInput != null && lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        else
        {
            // Fallback to direct mouse position for editor testing
            lookInput = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }

        Vector2 lookDelta = lookInput - previousLookInput;

        // Allow full 360 horizontal rotation
        targetRotation.y += lookDelta.x * horizontalSensitivity;

        // Limit vertical rotation if enabled
        if (limitVerticalRotation)
        {
            float newVertical = targetRotation.x - (lookDelta.y * verticalSensitivity);
            targetRotation.x = Mathf.Clamp(newVertical, minVerticalAngle, maxVerticalAngle);
        }
        else
        {
            targetRotation.x -= lookDelta.y * verticalSensitivity;
        }

        // Apply rotation
        if (useSmoothing)
        {
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * dampingStrength);
        }
        else
        {
            currentRotation = targetRotation;
        }

        transform.eulerAngles = currentRotation;
        previousLookInput = lookInput;
    }

    private void StartRotation(InputAction.CallbackContext context)
    {
        isRotating = true;
        previousLookInput = lookAction.ReadValue<Vector2>();

        // Store current rotation
        currentRotation = new Vector3(
            Mathf.Clamp(transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x, minVerticalAngle, maxVerticalAngle),
            transform.eulerAngles.y,
            0
        );
        targetRotation = currentRotation;

        if (showDebugRay)
        {
            Debug.Log("[VRReticlePointer] Rotation started");
        }
    }

    private void StopRotation(InputAction.CallbackContext context)
    {
        isRotating = false;

        if (showDebugRay)
        {
            Debug.Log("[VRReticlePointer] Rotation stopped");
        }
    }

    private void CheckInteractions()
    {
        if (mainCamera == null) return;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(
            mainCamera.transform.position,
            mainCamera.transform.forward,
            out hit,
            maxInteractionDistance,
            interactableLayers
        );

        if (showDebugRay)
        {
            if (hitSomething)
            {
                Debug.DrawLine(mainCamera.transform.position, hit.point, Color.green);
            }
            else
            {
                Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * maxInteractionDistance, Color.red);
            }
        }

        if (hitSomething)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if we hit a new object
            if (currentTarget != hitObject)
            {
                // Exit previous target
                if (currentTarget != null)
                {
                    TriggerPointerExit(currentTarget);
                }

                // Enter new target
                currentTarget = hitObject;
                TriggerPointerEnter(currentTarget);

                // NEW: Update reticle to show hover state
                if (reticle != null)
                {
                    reticle.SetHoverState(true);
                }
            }
        }
        else
        {
            // No hit - exit current target if any
            if (currentTarget != null)
            {
                TriggerPointerExit(currentTarget);
                currentTarget = null;

                // NEW: Update reticle to show idle state
                if (reticle != null)
                {
                    reticle.SetHoverState(false);
                }
            }
        }
    }

    private void HandleClick(InputAction.CallbackContext context)
    {
        if (currentTarget != null)
        {
            // NEW: Show click animation
            if (reticle != null)
            {
                reticle.SetActiveState(true);
            }

            // Trigger click event
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
            ExecuteEvents.Execute(currentTarget, eventData, ExecuteEvents.pointerClickHandler);

            if (showDebugRay)
            {
                Debug.Log($"[VRReticlePointer] Clicked on: {currentTarget.name}");
            }

            // Reset active state after brief delay
            Invoke(nameof(ResetActiveState), 0.1f);
        }
    }

    private void ResetActiveState()
    {
        if (reticle != null)
        {
            reticle.SetActiveState(false);
        }
    }

    private void TriggerPointerEnter(GameObject target)
    {
        if (target == null) return;

        // IMPROVED: Better error handling for UnityEvent invocation
        try
        {
            // Validate the event before invoking
            if (OnPointerEnter != null && OnPointerEnter.GetPersistentEventCount() > 0)
            {
                // Check if all persistent events are valid
                bool allEventsValid = true;
                for (int i = 0; i < OnPointerEnter.GetPersistentEventCount(); i++)
                {
                    if (OnPointerEnter.GetPersistentTarget(i) == null)
                    {
                        Debug.LogWarning($"OnPointerEnter event {i} has null target. Skipping event invocation.");
                        allEventsValid = false;
                        break;
                    }
                }

                if (allEventsValid)
                {
                    OnPointerEnter.Invoke(target);
                }
                else
                {
                    Debug.LogError($"OnPointerEnter has invalid event connections. Please check the Inspector and reassign missing scripts.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error invoking OnPointerEnter for {target.name}: {e.Message}");
            Debug.LogError($"This usually means there's a missing script or type mismatch in the Unity Events. Please check the Inspector.");
        }

        // Handle EventTrigger components with better error handling
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            try
            {
                // Create a properly initialized pointer event data
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = new Vector2(Screen.width / 2, Screen.height / 2); // Center of screen

                // Manually trigger the pointer enter using ExecuteEvents
                ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);

                // Check for EventTrigger component's entries
                if (eventTrigger.triggers != null)
                {
                    EventTrigger.Entry enterEntry = null;

                    // Find the pointer enter entry
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        if (entry.eventID == EventTriggerType.PointerEnter)
                        {
                            enterEntry = entry;
                            break;
                        }
                    }

                    // Invoke the callback if found and valid
                    if (enterEntry != null && enterEntry.callback != null)
                    {
                        // Validate the callback before invoking
                        if (enterEntry.callback.GetPersistentEventCount() > 0)
                        {
                            bool callbackValid = true;
                            for (int i = 0; i < enterEntry.callback.GetPersistentEventCount(); i++)
                            {
                                if (enterEntry.callback.GetPersistentTarget(i) == null)
                                {
                                    Debug.LogWarning($"EventTrigger PointerEnter callback {i} has null target on {target.name}");
                                    callbackValid = false;
                                    break;
                                }
                            }

                            if (callbackValid)
                            {
                                enterEntry.callback.Invoke(eventData);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error triggering EventTrigger pointer enter for {target.name}: {e.Message}");
                Debug.LogError($"This usually indicates a missing script or method in the EventTrigger configuration.");
            }
        }
    }

    private void TriggerPointerExit(GameObject target)
    {
        if (target == null) return;

        // IMPROVED: Better error handling for UnityEvent invocation
        try
        {
            // Validate the event before invoking
            if (OnPointerExit != null && OnPointerExit.GetPersistentEventCount() > 0)
            {
                // Check if all persistent events are valid
                bool allEventsValid = true;
                for (int i = 0; i < OnPointerExit.GetPersistentEventCount(); i++)
                {
                    if (OnPointerExit.GetPersistentTarget(i) == null)
                    {
                        Debug.LogWarning($"OnPointerExit event {i} has null target. Skipping event invocation.");
                        allEventsValid = false;
                        break;
                    }
                }

                if (allEventsValid)
                {
                    OnPointerExit.Invoke(target);
                }
                else
                {
                    Debug.LogError($"OnPointerExit has invalid event connections. Please check the Inspector and reassign missing scripts.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error invoking OnPointerExit for {target.name}: {e.Message}");
            Debug.LogError($"This usually means there's a missing script or type mismatch in the Unity Events. Please check the Inspector.");
        }

        // Handle EventTrigger components with better error handling
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            try
            {
                // Create a properly initialized pointer event data
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = new Vector2(Screen.width / 2, Screen.height / 2); // Center of screen

                // Manually trigger the pointer exit using ExecuteEvents
                ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

                // Check for EventTrigger component's entries
                if (eventTrigger.triggers != null)
                {
                    EventTrigger.Entry exitEntry = null;

                    // Find the pointer exit entry
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        if (entry.eventID == EventTriggerType.PointerExit)
                        {
                            exitEntry = entry;
                            break;
                        }
                    }

                    // Invoke the callback if found and valid
                    if (exitEntry != null && exitEntry.callback != null)
                    {
                        // Validate the callback before invoking
                        if (exitEntry.callback.GetPersistentEventCount() > 0)
                        {
                            bool callbackValid = true;
                            for (int i = 0; i < exitEntry.callback.GetPersistentEventCount(); i++)
                            {
                                if (exitEntry.callback.GetPersistentTarget(i) == null)
                                {
                                    Debug.LogWarning($"EventTrigger PointerExit callback {i} has null target on {target.name}");
                                    callbackValid = false;
                                    break;
                                }
                            }

                            if (callbackValid)
                            {
                                exitEntry.callback.Invoke(eventData);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error triggering EventTrigger pointer exit for {target.name}: {e.Message}");
                Debug.LogError($"This usually indicates a missing script or method in the EventTrigger configuration.");
            }
        }
    }

    public void SetMode(ViewMode newMode)
    {
        currentMode = newMode;
        ConfigureControls();

        // NEW: Show/hide reticle based on mode
        if (reticle != null)
        {
            // Always show reticle in 360 and VR modes
            reticle.SetVisible(newMode == ViewMode.Mode360 || newMode == ViewMode.ModeVR);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugRay && mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * maxInteractionDistance);
        }
    }

    // DIAGNOSTIC METHODS - Add these to help identify issues
    [ContextMenu("Diagnose Event Connections")]
    private void DiagnoseEventConnections()
    {
        Debug.Log("=== VRReticlePointer Event Diagnostics ===");

        if (OnPointerEnter != null)
        {
            Debug.Log($"OnPointerEnter has {OnPointerEnter.GetPersistentEventCount()} persistent events:");
            for (int i = 0; i < OnPointerEnter.GetPersistentEventCount(); i++)
            {
                var target = OnPointerEnter.GetPersistentTarget(i);
                var methodName = OnPointerEnter.GetPersistentMethodName(i);
                Debug.Log($"  Event {i}: Target = {(target != null ? target.name : "NULL")}, Method = {methodName}");
            }
        }
        else
        {
            Debug.Log("OnPointerEnter is null");
        }

        if (OnPointerExit != null)
        {
            Debug.Log($"OnPointerExit has {OnPointerExit.GetPersistentEventCount()} persistent events:");
            for (int i = 0; i < OnPointerExit.GetPersistentEventCount(); i++)
            {
                var target = OnPointerExit.GetPersistentTarget(i);
                var methodName = OnPointerExit.GetPersistentMethodName(i);
                Debug.Log($"  Event {i}: Target = {(target != null ? target.name : "NULL")}, Method = {methodName}");
            }
        }
        else
        {
            Debug.Log("OnPointerExit is null");
        }
    }

    #region Public API for Reticle Customization

    /// <summary>
    /// NEW: Customize reticle colors
    /// </summary>
    public void SetReticleColors(Color idle, Color hover, Color active)
    {
        if (reticle != null)
        {
            reticle.SetColors(idle, hover, active);
        }
    }

    /// <summary>
    /// NEW: Customize reticle sizes (in pixels)
    /// </summary>
    public void SetReticleSizes(float dotSize, float circleSize, float ringThickness)
    {
        if (reticle != null)
        {
            reticle.SetSizes(dotSize, circleSize, ringThickness);
        }
    }

    /// <summary>
    /// NEW: Change reticle shape
    /// </summary>
    public void SetReticleShape(UIReticlePointer.ReticleShape shape)
    {
        if (reticle != null)
        {
            reticle.SetReticleShape(shape);
        }
    }

    #endregion
}