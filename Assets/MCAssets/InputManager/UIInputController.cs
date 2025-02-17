using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UIInputController : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction clickAction;
    private Camera mainCamera;

    private void Awake()
    {
        // Set up input system
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }

        // Get the main camera
        mainCamera = Camera.main;

        // Get reference to our click action
        clickAction = playerInput.actions["Click"];
    }

    private void OnEnable()
    {
        clickAction.performed += OnClick;
    }

    private void OnDisable()
    {
        clickAction.performed -= OnClick;
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // Get the current pointer position
        Vector2 pointerPosition = GetPointerPosition();

        // Check if we hit a UI element
        if (IsPointerOverUI(pointerPosition))
        {
            // Handle the UI interaction
            HandleUIInteraction(pointerPosition);
        }
    }

    private Vector2 GetPointerPosition()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // For touch input
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
        }
        
        // For mouse input
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }

    private bool IsPointerOverUI(Vector2 position)
    {
        // Create a pointer event data
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = position;

        // Raycast against the UI
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Return true if we hit any UI elements
        return results.Count > 0;
    }

    private void HandleUIInteraction(Vector2 position)
    {
        // Create a pointer event data
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = position;

        // Raycast against the UI
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // Check if the UI element has a button component
            UnityEngine.UI.Button button = result.gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                break;
            }
        }
    }
}
