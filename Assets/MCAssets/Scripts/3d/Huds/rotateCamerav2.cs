using UnityEngine;

public class rotateCamerav2 : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Transform player; // The player object to rotate
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 50f; // How fast the player rotates
    public float delay = 3f; // Delay before rotation starts

    private float hoverTimer = 0f; // Timer to track hover duration
    private bool isHovering = false; // Is the reticle hovering over the icon?
    private bool isRotating = false; // Is the player currently rotating?
    private float rotationDirection = 0f; // Direction of rotation (1 for clockwise, -1 for counterclockwise)

    [Header("Icon Settings")]
    public ToggleActiveIcons toggleActiveIcons; // Script for updating icon visuals

    private void Update()
    {
        // If hovering over the icon
        if (isHovering)
        {
            hoverTimer += Time.deltaTime; // Increment the hover timer

            // If the delay has passed, start rotating
            if (hoverTimer >= delay && !isRotating)
            {
                isRotating = true; // Set the rotating state
                toggleActiveIcons?.SelectIcon(); // Update the icon to the selected state
            }
            else if (!isRotating)
            {
                toggleActiveIcons?.HoverIcon(); // Update the icon to the hover state
            }

            // Rotate the player if the rotation is active
            if (isRotating)
            {
                RotatePlayer();
            }
        }
    }

    private void RotatePlayer()
    {
        if (player != null)
        {
            // Rotate the player on the Y-axis based on direction and speed
            player.Rotate(Vector3.up * rotationDirection * rotationSpeed * Time.deltaTime);
        }
    }

    public void OnMouseHoverEnter(float direction)
    {
        // Called when the reticle enters the icon
        isHovering = true;
        rotationDirection = direction; // Set the rotation direction
        hoverTimer = 0f; // Reset the hover timer
    }

    public void OnMouseHoverExit()
    {
        // Called when the reticle exits the icon
        isHovering = false;
        isRotating = false; // Stop rotating
        hoverTimer = 0f; // Reset the hover timer
        rotationDirection = 0f; // Clear the rotation direction
        toggleActiveIcons?.DefaultIcon(); // Update the icon to the default state
    }
}
