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

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction touchPressAction;
    private InputAction clickAction;

    private Camera mainCamera;
    private GameObject currentTarget;
    private WorldSpaceFocusIndicator focusIndicator;

    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainCamera = GetComponentInChildren<Camera>();
        focusIndicator = GetComponent<WorldSpaceFocusIndicator>();

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

        if (Physics.Raycast(ray, out hitInfo, maxInteractionDistance, interactableLayers))
        {
            HandleTargetInteraction(hitInfo.collider.gameObject);
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    private void HandleTargetInteraction(GameObject hitObject)
    {
        if (hitObject.GetComponent<BoxCollider>() != null && ((1 << hitObject.layer) & interactableLayers) != 0)
        {
            if (currentTarget != hitObject)
            {
                ClearCurrentTarget();
                currentTarget = hitObject;
                TriggerPointerEnter(hitObject);

                // Update focus indicator state
                focusIndicator?.SetInteractiveState(true);
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
            focusIndicator?.SetInteractiveState(false);
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
}