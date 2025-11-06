using UnityEngine;

/// <summary>
/// Handles player rotation for VR HUD controls.
/// Attach to Player GameObject (with CharacterController).
/// Called by HUD buttons to rotate left/right.
/// </summary>
public class VRPlayerRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 30f; // Degrees per second
    [SerializeField] private float rotationAmount = 45f; // Degrees per button press
    [SerializeField] private bool smoothRotation = true;

    [Header("Camera References")]
    [SerializeField] private Transform camera360;
    [SerializeField] private Transform cameraVR;

    // State
    private bool isRotating = false;
    private float targetYRotation;
    private float currentRotationVelocity;

    void Start()
    {
        targetYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        if (isRotating && smoothRotation)
        {
            // Smooth rotation toward target
            float currentY = transform.eulerAngles.y;
            float newY = Mathf.SmoothDampAngle(
                currentY,
                targetYRotation,
                ref currentRotationVelocity,
                0.2f,
                rotationSpeed
            );

            Vector3 newRotation = transform.eulerAngles;
            newRotation.y = newY;
            transform.eulerAngles = newRotation;

            // Stop rotating when close to target
            if (Mathf.Abs(Mathf.DeltaAngle(currentY, targetYRotation)) < 0.1f)
            {
                isRotating = false;
            }
        }
    }

    /// <summary>
    /// Rotate player left (called by HUD button)
    /// </summary>
    public void RotateLeft()
    {
        targetYRotation -= rotationAmount;

        if (smoothRotation)
        {
            isRotating = true;
        }
        else
        {
            // Instant rotation
            Vector3 newRotation = transform.eulerAngles;
            newRotation.y = targetYRotation;
            transform.eulerAngles = newRotation;
        }

        Debug.Log($"VRPlayerRotation: Rotating left to {targetYRotation}°");
    }

    /// <summary>
    /// Rotate player right (called by HUD button)
    /// </summary>
    public void RotateRight()
    {
        targetYRotation += rotationAmount;

        if (smoothRotation)
        {
            isRotating = true;
        }
        else
        {
            // Instant rotation
            Vector3 newRotation = transform.eulerAngles;
            newRotation.y = targetYRotation;
            transform.eulerAngles = newRotation;
        }

        Debug.Log($"VRPlayerRotation: Rotating right to {targetYRotation}°");
    }

    /// <summary>
    /// Rotate player to specific angle
    /// </summary>
    public void RotateToAngle(float angle)
    {
        targetYRotation = angle;

        if (smoothRotation)
        {
            isRotating = true;
        }
        else
        {
            Vector3 newRotation = transform.eulerAngles;
            newRotation.y = targetYRotation;
            transform.eulerAngles = newRotation;
        }
    }

    /// <summary>
    /// Set rotation speed
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Clamp(speed, 10f, 180f);
    }

    /// <summary>
    /// Set rotation amount per button press
    /// </summary>
    public void SetRotationAmount(float amount)
    {
        rotationAmount = Mathf.Clamp(amount, 15f, 90f);
    }
}