using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// FIXED VERSION: Critical bug fix - now properly checks interactions in VR mode!
/// 
/// BUG FIX: Previous version had "if (currentMode == ViewMode.ModeVR) return;" which prevented
/// CheckInteractions() from running in VR mode, causing buttons to not respond.
/// 
/// This version ALWAYS runs CheckInteractions() regardless of mode.
/// 
/// Handles raycasting and interaction logic for VR and 360 modes.
/// </summary>
public class VRReticlePointerFixed : MonoBehaviour
{
    public enum ViewMode { Mode2D, Mode360, ModeVR }

    [Header("Mode Settings")]
    [SerializeField] private ViewMode currentMode = ViewMode.ModeVR; // Default to VR mode
    [SerializeField] private bool showDebugRay = true;

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

    private UIReticlePointer reticle;

    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private bool useInputSystemFallback = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();

        Debug.Log($"<color=cyan>[VRReticlePointerFixed] Awake - Camera found: {mainCamera != null}</color>");
        if (mainCamera != null)
        {
            Debug.Log($"<color=cyan>[VRReticlePointerFixed] Camera name: {mainCamera.name}, Position: {mainCamera.transform.position}</color>");
        }

        SetupReticle();

        if (playerInput != null)
        {
            try
            {
                lookAction = playerInput.actions["Look"];
                touchPressAction = playerInput.actions["TouchPress"];
                clickAction = playerInput.actions["Click"];

                Debug.Log("[VRReticlePointerFixed] Using Input System actions");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VRReticlePointerFixed] Input Actions not configured: {e.Message}");
                Debug.LogWarning("[VRReticlePointerFixed] Falling back to direct mouse input for editor testing");
                useInputSystemFallback = true;
                playerInput = null;
            }
        }
        else
        {
            Debug.LogWarning("[VRReticlePointerFixed] No PlayerInput component - using mouse input fallback");
            useInputSystemFallback = true;
        }

        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;

        ConfigureControls();
    }

    private void Start()
    {
        if (reticle != null)
        {
            reticle.SetHoverState(false);
            reticle.SetActiveState(false);

            Debug.Log("[VRReticlePointerFixed] Reticle initialized to idle state");
        }

        Debug.Log($"<color=lime>[VRReticlePointerFixed] ✓ INITIALIZED - Mode: {currentMode}, MaxDist: {maxInteractionDistance}, Layers: {LayerMaskToString(interactableLayers)}</color>");
        Debug.Log($"<color=lime>[VRReticlePointerFixed] ✓ BUG FIX ACTIVE: Will check interactions in ALL modes including VR!</color>");
    }

    private void SetupReticle()
    {
        reticle = GetComponent<UIReticlePointer>();

        if (reticle == null && createReticleAutomatically)
        {
            reticle = gameObject.AddComponent<UIReticlePointer>();
            Debug.Log("[VRReticlePointerFixed] UIReticlePointer created automatically");
        }
        else if (reticle != null)
        {
            Debug.Log("[VRReticlePointerFixed] Using existing UIReticlePointer");
        }
        else
        {
            Debug.LogWarning("[VRReticlePointerFixed] No reticle will be displayed - add UIReticlePointer manually if needed");
        }
    }

    private void ConfigureControls()
    {
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

    // ============================================
    // CRITICAL FIX: Removed early return in VR mode
    // ============================================
    private void LateUpdate()
    {
        // FIXED: Previous version had "if (currentMode == ViewMode.ModeVR) return;" here
        // which prevented CheckInteractions() from running in VR mode!

        // Handle rotation in 360 mode only
        if (currentMode == ViewMode.Mode360)
        {
            if (useInputSystemFallback || playerInput == null)
            {
                HandleMouseInput();
            }

            if (isRotating)
            {
                HandleRotation();
            }
        }

        // CRITICAL: Always check interactions regardless of mode
        CheckInteractions();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(1))
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

    private void StartRotation(InputAction.CallbackContext context)
    {
        isRotating = true;
        Vector2 touchPosition = context.ReadValue<Vector2>();
        previousLookInput = touchPosition;

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

    private void HandleRotation()
    {
        Vector2 lookInput;

        if (useInputSystemFallback || playerInput == null)
        {
            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            lookInput = currentMousePos - previousLookInput;
            previousLookInput = currentMousePos;
        }
        else if (lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        else
        {
            return;
        }

        if (lookInput.magnitude > 0.001f)
        {
            targetRotation.y += lookInput.x * horizontalSensitivity;
            targetRotation.x -= lookInput.y * verticalSensitivity;

            if (limitVerticalRotation)
            {
                targetRotation.x = Mathf.Clamp(targetRotation.x, minVerticalAngle, maxVerticalAngle);
            }

            if (useSmoothing)
            {
                currentRotation = Vector3.Lerp(currentRotation, targetRotation, dampingStrength * Time.deltaTime);
            }
            else
            {
                currentRotation = targetRotation;
            }

            transform.eulerAngles = currentRotation;
        }
    }

    private void HandleClick(InputAction.CallbackContext context)
    {
        Debug.Log("[VRReticlePointerFixed] Click detected in 2D mode");
    }

    // ============================================
    // INTERACTION SYSTEM WITH DETAILED LOGGING
    // ============================================
    private void CheckInteractions()
    {
        if (mainCamera == null) return;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxInteractionDistance, Color.green, 0.1f);
        }

        bool hitSomething = Physics.Raycast(ray, out hit, maxInteractionDistance, interactableLayers);

        // Periodic debug logging
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=cyan>[RAYCAST]</color> Hit: {hitSomething}, Distance: {(hitSomething ? hit.distance.ToString("F2") : "N/A")}, Target: {(hitSomething ? hit.collider.gameObject.name : "None")}");
        }

        if (hitSomething)
        {
            GameObject target = hit.collider.gameObject;

            // Log new target detection
            if (target != currentTarget)
            {
                Debug.Log($"<color=yellow>[HIT]</color> New target: {target.name} on layer {LayerMask.LayerToName(target.layer)} at {hit.distance:F2}m");
            }

            // Update reticle visual state
            if (reticle != null)
            {
                reticle.SetHoverState(true);
            }

            if (target != currentTarget)
            {
                if (currentTarget != null)
                {
                    Debug.Log($"<color=orange>[EXIT]</color> Leaving: {currentTarget.name}");
                    TriggerPointerExit(currentTarget);
                }

                currentTarget = target;
                Debug.Log($"<color=green>[ENTER]</color> Entering: {target.name}");
                TriggerPointerEnter(target);
            }
        }
        else
        {
            // Update reticle to idle state
            if (reticle != null)
            {
                reticle.SetHoverState(false);
            }

            if (currentTarget != null)
            {
                Debug.Log($"<color=orange>[EXIT]</color> No hit, leaving: {currentTarget.name}");
                TriggerPointerExit(currentTarget);
                currentTarget = null;
            }
        }
    }

    private void TriggerPointerEnter(GameObject target)
    {
        if (target == null) return;

        Debug.Log($"<color=lime>[TRIGGER ENTER]</color> Triggering pointer enter on: {target.name}");

        // Try UnityEvent first
        try
        {
            if (OnPointerEnter != null && OnPointerEnter.GetPersistentEventCount() > 0)
            {
                OnPointerEnter.Invoke(target);
                Debug.Log($"<color=lime>[TRIGGER ENTER]</color> UnityEvent invoked");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TRIGGER ENTER] Error invoking UnityEvent: {e.Message}");
        }

        // Try IPointerEnterHandler interface (THIS IS WHAT WE NEED!)
        var enterHandlers = target.GetComponents<IPointerEnterHandler>();
        if (enterHandlers.Length > 0)
        {
            Debug.Log($"<color=lime>[TRIGGER ENTER]</color> Found {enterHandlers.Length} IPointerEnterHandler(s)");
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);

            foreach (var handler in enterHandlers)
            {
                Debug.Log($"<color=lime>[TRIGGER ENTER]</color> Calling OnPointerEnter on: {handler.GetType().Name}");
                handler.OnPointerEnter(eventData);
            }
        }
        else
        {
            Debug.LogWarning($"<color=red>[TRIGGER ENTER]</color> No IPointerEnterHandler found on {target.name}");
        }

        // Try EventTrigger components
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
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        if (entry.eventID == EventTriggerType.PointerEnter && entry.callback != null)
                        {
                            entry.callback.Invoke(eventData);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TRIGGER ENTER] Error with EventTrigger: {e.Message}");
            }
        }
    }

    private void TriggerPointerExit(GameObject target)
    {
        if (target == null) return;

        Debug.Log($"<color=orange>[TRIGGER EXIT]</color> Triggering pointer exit on: {target.name}");

        // Try UnityEvent
        try
        {
            if (OnPointerExit != null && OnPointerExit.GetPersistentEventCount() > 0)
            {
                OnPointerExit.Invoke(target);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TRIGGER EXIT] Error invoking UnityEvent: {e.Message}");
        }

        // Try IPointerExitHandler interface
        var exitHandlers = target.GetComponents<IPointerExitHandler>();
        if (exitHandlers.Length > 0)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);

            foreach (var handler in exitHandlers)
            {
                Debug.Log($"<color=orange>[TRIGGER EXIT]</color> Calling OnPointerExit on: {handler.GetType().Name}");
                handler.OnPointerExit(eventData);
            }
        }

        // Try EventTrigger
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
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        if (entry.eventID == EventTriggerType.PointerExit && entry.callback != null)
                        {
                            entry.callback.Invoke(eventData);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TRIGGER EXIT] Error with EventTrigger: {e.Message}");
            }
        }
    }

    public void SetMode(ViewMode newMode)
    {
        currentMode = newMode;
        ConfigureControls();

        if (reticle != null)
        {
            reticle.SetVisible(newMode == ViewMode.Mode360 || newMode == ViewMode.ModeVR);
        }

        Debug.Log($"<color=cyan>[VRReticlePointerFixed] Mode changed to: {newMode}</color>");
    }

    private void OnDrawGizmos()
    {
        if (showDebugRay && mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * maxInteractionDistance);
        }
    }

    private string LayerMaskToString(LayerMask mask)
    {
        if (mask == -1) return "Everything";
        if (mask == 0) return "Nothing";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(LayerMask.LayerToName(i));
            }
        }
        return sb.Length > 0 ? sb.ToString() : "Nothing";
    }

    #region Public API for Reticle Customization

    public void SetReticleColors(Color idle, Color hover, Color active)
    {
        if (reticle != null)
        {
            reticle.SetColors(idle, hover, active);
        }
    }

    public void SetReticleSizes(float dotSize, float circleSize, float ringThickness)
    {
        if (reticle != null)
        {
            reticle.SetSizes(dotSize, circleSize, ringThickness);
        }
    }

    public void SetReticleShape(UIReticlePointer.ReticleShape shape)
    {
        if (reticle != null)
        {
            reticle.SetReticleShape(shape);
        }
    }

    #endregion
}