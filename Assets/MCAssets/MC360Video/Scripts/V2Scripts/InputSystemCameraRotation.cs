using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Camera rotation using New Input System.
/// Supports Mouse (editor) and Touch (device).
/// Attach to MainCamera360 and MainCameraVR.
/// </summary>
public class InputSystemCameraRotation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private bool invertY = false;

    private Vector3 rotation;
    private Mouse mouse;
    private bool touchEnabled = false;

    void OnEnable()
    {
        // Enable enhanced touch
        EnhancedTouchSupport.Enable();
        touchEnabled = true;

        mouse = Mouse.current;
        rotation = transform.eulerAngles;

        Debug.Log("[InputSystemCameraRotation] Initialized");
    }

    void OnDisable()
    {
        if (touchEnabled)
        {
            EnhancedTouchSupport.Disable();
            touchEnabled = false;
        }
    }

    void Update()
    {
        // Try touch first (for device)
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            Vector2 delta = touch.delta;

            if (delta.magnitude > 0.01f)
            {
                rotation.y += delta.x * sensitivity;
                rotation.x += (invertY ? delta.y : -delta.y) * sensitivity;

                rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);
                transform.eulerAngles = rotation;
            }

            return; // Touch handled, skip mouse
        }

        // Mouse input (for editor)
        if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();

            if (delta.magnitude > 0.01f)
            {
                rotation.y += delta.x * sensitivity;
                rotation.x += (invertY ? delta.y : -delta.y) * sensitivity;

                rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);
                transform.eulerAngles = rotation;
            }
        }
    }
}