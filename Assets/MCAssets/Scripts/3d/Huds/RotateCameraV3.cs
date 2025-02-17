using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RotateCameraV3 : MonoBehaviour
{
    [Header("Camera Movement Settings")]
    [SerializeField, Range(0.1f, 20f)] private float horizontalSensitivity = 2f;
    [SerializeField] private float dampingStrength = 5f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Vector3 rotationDirection = Vector3.up;

    [Header("Interaction Settings")]
    [SerializeField] private float maxInteractionDistance = 10f;
    [SerializeField] private LayerMask interactableLayers = -1;

    private PlayerInput playerInput;
    private InputAction lookAction;
    private Camera mainCamera;
    private GameObject currentTarget;
    private bool isRotating = false;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

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
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;
    }

    private void Update()
    {
        if (isRotating)
        {
            HandleRotation();
        }

        CheckForInteractables();
    }

    private void HandleRotation()
    {
        targetRotation += rotationDirection * (rotationSpeed * Time.deltaTime);

        if (useSmoothing)
        {
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * dampingStrength);
            transform.rotation = Quaternion.Euler(currentRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(targetRotation);
        }
    }

    public void SetRotationState(bool isActive)
    {
        isRotating = isActive;
    }

    private void CheckForInteractables()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxInteractionDistance, interactableLayers))
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
        EventTrigger eventTrigger = hitObject.GetComponent<EventTrigger>();

        if (eventTrigger != null)
        {
            if (currentTarget != hitObject)
            {
                ClearCurrentTarget();
                currentTarget = hitObject;
                TriggerPointerEnter(hitObject);
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
        }
    }

    private void TriggerPointerEnter(GameObject target)
    {
        SetRotationState(true);
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            var enterEntry = eventTrigger.triggers.Find(trigger => trigger.eventID == EventTriggerType.PointerEnter);
            if (enterEntry != null)
            {
                var eventData = new PointerEventData(EventSystem.current);
                enterEntry.callback.Invoke(eventData);
            }
        }
    }

    private void TriggerPointerExit(GameObject target)
    {
        SetRotationState(false);
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            var exitEntry = eventTrigger.triggers.Find(trigger => trigger.eventID == EventTriggerType.PointerExit);
            if (exitEntry != null)
            {
                var eventData = new PointerEventData(EventSystem.current);
                exitEntry.callback.Invoke(eventData);
            }
        }
    }
}
