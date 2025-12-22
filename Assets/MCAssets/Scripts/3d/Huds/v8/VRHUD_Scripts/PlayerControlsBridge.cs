using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads your existing PlayerControls InputActionAsset and exposes a consistent LookDelta signal.
/// This avoids duplicating input logic across systems.
///
/// Your asset (per your screenshots):
/// Action Map: Player
/// Actions:
/// - Look (Value/Vector2) bound to Mouse Position + Touch Position (ABSOLUTE screen coords)
/// - TouchPress (Button) bound to Mouse Right Button + Touch Press (drag held)
/// - Click (Button) exists but is NOT used for HUD selection yet (reticle+dwell only).
///
/// This script computes delta from absolute Look position and gates it behind TouchPress (right mouse held),
/// so you can right-click drag to look around in editor testing.
/// </summary>
public class PlayerControlsBridge : MonoBehaviour
{
    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset actions;

    [Header("Action Names (match your PlayerControls)")]
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string lookActionName = "Look";           // Vector2
    [SerializeField] private string dragHoldActionName = "TouchPress";  // Button
    [SerializeField] private string clickActionName = "Click";          // Button (not used yet)

    [Header("Look Interpretation")]
    [SerializeField] private bool lookIsAbsolutePosition = true;
    [SerializeField] private bool requireDragHoldForLook = true;

    public Vector2 LookDelta { get; private set; }
    public bool DragHeld { get; private set; }

    private InputAction lookAction;
    private InputAction dragHoldAction;
    private InputAction clickAction;

    private Vector2 lastLook;
    private bool hasLastLook;

    private void OnEnable()
    {
        ResolveActions();
        lookAction?.Enable();
        dragHoldAction?.Enable();
        clickAction?.Enable();

        hasLastLook = false;
        lastLook = Vector2.zero;
    }

    private void OnDisable()
    {
        lookAction?.Disable();
        dragHoldAction?.Disable();
        clickAction?.Disable();
    }

    private void Update()
    {
        DragHeld = dragHoldAction != null && dragHoldAction.IsPressed();

        Vector2 lookValue = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        Vector2 delta;
        if (lookIsAbsolutePosition)
        {
            if (!hasLastLook)
            {
                lastLook = lookValue;
                hasLastLook = true;
                delta = Vector2.zero;
            }
            else
            {
                delta = lookValue - lastLook;
                lastLook = lookValue;
            }
        }
        else
        {
            delta = lookValue;
        }

        if (requireDragHoldForLook && !DragHeld)
            delta = Vector2.zero;

        LookDelta = delta;
    }

    private void ResolveActions()
    {
        lookAction = null;
        dragHoldAction = null;
        clickAction = null;

        if (actions == null) return;

        var map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null) return;

        lookAction = map.FindAction(lookActionName, throwIfNotFound: false);
        dragHoldAction = map.FindAction(dragHoldActionName, throwIfNotFound: false);
        clickAction = map.FindAction(clickActionName, throwIfNotFound: false);
    }
}
