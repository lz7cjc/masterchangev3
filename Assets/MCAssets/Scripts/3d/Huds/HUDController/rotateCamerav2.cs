using UnityEngine;

/// <summary>
/// FIXED rotateCamerav2 - Works with GazeHoverTrigger system
/// Rotates camera continuously while gaze is hovering over icon
/// </summary>
public class rotateCamerav2 : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Transform player; // The player object to rotate
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 50f; // How fast the player rotates
    public float delay = 3f; // NOT USED - GazeHoverTrigger handles delay

    private bool isRotating = false; // Is the player currently rotating?
    private float rotationDirection = 0f; // Direction of rotation (1 for clockwise, -1 for counterclockwise)

    [Header("Icon Settings")]
    public ToggleActiveIcons toggleActiveIcons; // Script for updating icon visuals

    private void Update()
    {
        // Rotate the player if rotation is active
        if (isRotating && player != null)
        {
            // Rotate the player on the Y-axis based on direction and speed
            player.Rotate(Vector3.up * rotationDirection * rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Called by GazeRotationAdapter when hover completes (after countdown)
    /// Starts continuous rotation
    /// </summary>
    public void OnMouseHoverEnter(float direction)
    {
        Debug.Log($"[rotateCamerav2] Starting rotation - Direction: {direction}");
        
        isRotating = true;
        rotationDirection = direction; // Set the rotation direction
        
        // Update icon to selected state
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }
    }

    /// <summary>
    /// Called by GazeRotationAdapter when gaze exits
    /// Stops rotation
    /// </summary>
    public void OnMouseHoverExit()
    {
        Debug.Log($"[rotateCamerav2] Stopping rotation");
        
        isRotating = false;
        
        // Reset icon to default state
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.DefaultIcon();
        }
    }

    /// <summary>
    /// Public method to check if currently rotating
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }
}
