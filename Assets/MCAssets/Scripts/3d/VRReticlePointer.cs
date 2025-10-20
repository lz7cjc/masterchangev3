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

    private SpriteBasedFocusIndicator focusIndicator;

    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();

        focusIndicator = GetComponent<SpriteBasedFocusIndicator>();

        if (focusIndicator == null)
        {
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
        // CRITICAL FIX: Removed the line that was preventing VR interactions!
        // The original code had: if (currentMode == ViewMode.ModeVR) return;
        // This was blocking ALL interactions in VR mode!

        if (currentMode == ViewMode.Mode360 && isRotating)
        {
            HandleRotation();
        }

        // IMPORTANT: Always check interactions, including in VR mode
        CheckInteractions();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        Vector2 lookDelta = lookInput - previousLookInput;

        targetRotation.y += lookDelta.x * horizontalSensitivity;

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

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxInteractionDistance, Color.yellow);
        }

        if (Physics.Raycast(ray, out hitInfo, maxInteractionDistance, interactableLayers))
        {
            if (showDebugRay)
            {
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
        bool isInteractable = false;

        BoxCollider boxCollider = hitObject.GetComponent<BoxCollider>();
        if (boxCollider != null && ((1 << hitObject.layer) & interactableLayers) != 0)
        {
            isInteractable = true;
        }

        EventTrigger eventTrigger = hitObject.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            isInteractable = true;
        }

        if (isInteractable)
        {
            if (currentTarget != hitObject)
            {
                ClearCurrentTarget();
                currentTarget = hitObject;
                TriggerPointerEnter(hitObject);

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

            if (focusIndicator != null)
            {
                focusIndicator.SetInteractiveState(false);
            }
        }
    }

    private void TriggerPointerEnter(GameObject target)
    {
        if (target == null) return;

        Debug.Log($"VRReticlePointer: TriggerPointerEnter called for {target.name} in {currentMode} mode");

        // Method 1: Try UnityEvent first (your existing setup should work)
        try
        {
            if (OnPointerEnter != null && OnPointerEnter.GetPersistentEventCount() > 0)
            {
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
                    Debug.Log("✅ Successfully invoked OnPointerEnter UnityEvent");
                }
                else
                {
                    Debug.LogError("❌ OnPointerEnter has invalid event connections. Please check the Inspector and reassign missing scripts.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error invoking OnPointerEnter for {target.name}: {e.Message}");
        }

        // Method 2: Handle EventTrigger components
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            try
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);

                ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);

                if (eventTrigger.triggers != null)
                {
                    EventTrigger.Entry enterEntry = null;
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        if (entry.eventID == EventTriggerType.PointerEnter)
                        {
                            enterEntry = entry;
                            break;
                        }
                    }

                    if (enterEntry != null && enterEntry.callback != null)
                    {
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
                                Debug.Log("✅ Successfully invoked EventTrigger PointerEnter");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error triggering EventTrigger pointer enter for {target.name}: {e.Message}");
            }
        }


    }

    private void TriggerPointerExit(GameObject target)
    {
        if (target == null) return;

        Debug.Log($"VRReticlePointer: TriggerPointerExit called for {target.name} in {currentMode} mode");

        // Method 1: Try UnityEvent first
        try
        {
            if (OnPointerExit != null && OnPointerExit.GetPersistentEventCount() > 0)
            {
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
                    Debug.Log("✅ Successfully invoked OnPointerExit UnityEvent");
                }
                else
                {
                    Debug.LogError("❌ OnPointerExit has invalid event connections.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error invoking OnPointerExit for {target.name}: {e.Message}");
        }

        // Method 2: Handle EventTrigger components
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            try
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);

                ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);

                if (eventTrigger.triggers != null)
                {
                    var exitEntry = eventTrigger.triggers.Find(entry => entry.eventID == EventTriggerType.PointerExit);
                    if (exitEntry != null && exitEntry.callback != null)
                    {
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
                                Debug.Log("✅ Successfully invoked EventTrigger PointerExit");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error triggering EventTrigger pointer exit for {target.name}: {e.Message}");
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

    [ContextMenu("Diagnose Event Connections")]
    private void DiagnoseEventConnections()
    {
        Debug.Log("=== VRReticlePointer Event Diagnostics ===");
        Debug.Log($"Current Mode: {currentMode}");
        Debug.Log($"Platform: {(Application.isMobilePlatform ? "Mobile" : "Desktop")}");
        Debug.Log($"EventSystem.current: {(EventSystem.current != null ? "Found" : "NULL")}");

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

        if (currentTarget != null)
        {
            Debug.Log($"Current target: {currentTarget.name}");

            EventTrigger eventTrigger = currentTarget.GetComponent<EventTrigger>();
            if (eventTrigger != null)
            {
                Debug.Log($"  - ✅ Has EventTrigger with {eventTrigger.triggers.Count} triggers");
            }
        }
        else
        {
            Debug.Log("No current target");
        }
    }
}