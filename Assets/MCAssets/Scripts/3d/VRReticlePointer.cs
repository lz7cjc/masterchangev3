using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;

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

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> OnPointerEnter;
    [SerializeField] private UnityEvent<GameObject> OnPointerExit;

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction touchPressAction;
    private InputAction clickAction;

    private Camera mainCamera;
    private GameObject currentTarget;

    // UPDATED: Changed to look for SpriteBasedFocusIndicator instead
    private SpriteBasedFocusIndicator focusIndicator;

    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();

        // UPDATED: Changed to get SpriteBasedFocusIndicator
        focusIndicator = GetComponent<SpriteBasedFocusIndicator>();

        if (focusIndicator == null)
        {
            // Try adding the component if it doesn't exist
            focusIndicator = gameObject.AddComponent<SpriteBasedFocusIndicator>();
            Debug.Log("Added SpriteBasedFocusIndicator component");
        }

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

        ConfigureControls();
    }

    private void ConfigureControls()
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

        if (currentMode == ViewMode.Mode360 && isRotating)
        {
            HandleRotation();
        }

        CheckInteractions();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        Vector2 lookDelta = lookInput - previousLookInput;

        // Allow full 360 horizontal rotation
        targetRotation.y += lookDelta.x * horizontalSensitivity;

        // Limit vertical rotation if enabled
        if (limitVerticalRotation)
        {
            targetRotation.x = Mathf.Clamp(targetRotation.x - lookDelta.y * verticalSensitivity, minVerticalAngle, maxVerticalAngle);
        }
        else
        {
            targetRotation.x -= lookDelta.y * verticalSensitivity;
        }

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
            Mathf.Clamp(transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x, minVerticalAngle, maxVerticalAngle),
            transform.eulerAngles.y,
            0
        );
        targetRotation = currentRotation;
    }

    private void StopRotation(InputAction.CallbackContext context)
    {
        isRotating = false;
    }

    private void CheckInteractions()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hitInfo;

        // Draw debug ray
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxInteractionDistance, Color.yellow);
        }

        if (Physics.Raycast(ray, out hitInfo, maxInteractionDistance, interactableLayers))
        {
            if (showDebugRay)
            {
                // Draw a hit point marker
                Debug.DrawLine(ray.origin, hitInfo.point, Color.green);
            }
            HandleTargetInteraction(hitInfo.collider.gameObject);
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    private void HandleTargetInteraction(GameObject hitObject)
    {
        // Check if this is an interactable object
        bool isInteractable = false;

        // Consider object interactable if it has a BoxCollider on the correct layer
        BoxCollider boxCollider = hitObject.GetComponent<BoxCollider>();
        if (boxCollider != null && ((1 << hitObject.layer) & interactableLayers) != 0)
        {
            isInteractable = true;
        }

        // Also consider objects with EventTrigger components as interactable
        EventTrigger eventTrigger = hitObject.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            isInteractable = true;
        }

        // Handle the target interaction based on interactability
        if (isInteractable)
        {
            if (currentTarget != hitObject)
            {
                ClearCurrentTarget();
                currentTarget = hitObject;
                TriggerPointerEnter(hitObject);

                // Update focus indicator state
                if (focusIndicator != null)
                {
                    focusIndicator.SetInteractiveState(true);
                }
            }
        }
        else if (currentTarget != null)
        {
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

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            TriggerPointerExit(currentTarget);
            currentTarget = null;

            // Reset focus indicator state
            if (focusIndicator != null)
            {
                focusIndicator.SetInteractiveState(false);
            }
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
}