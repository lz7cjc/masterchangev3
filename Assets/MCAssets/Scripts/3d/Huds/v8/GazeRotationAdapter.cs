using UnityEngine;

/// <summary>
/// GazeRotationAdapter - Bridges GazeHoverTrigger and rotateCamerav2
/// Allows rotateCamerav2 to work with the GazeHoverTrigger system
/// Attach this to the same GameObject as rotateCamerav2
/// </summary>
[RequireComponent(typeof(rotateCamerav2))]
public class GazeRotationAdapter : MonoBehaviour
{
    [Header("Rotation Direction")]
    [Tooltip("1 for clockwise (right), -1 for counterclockwise (left)")]
    [SerializeField] private float rotationDirection = 1f;

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectDirection = true;

    private rotateCamerav2 rotationScript;

    void Awake()
    {
        rotationScript = GetComponent<rotateCamerav2>();

        // Auto-detect direction from GameObject name
        if (autoDetectDirection)
        {
            string objName = gameObject.name.ToLower();
            if (objName.Contains("left"))
            {
                rotationDirection = -1f;
            }
            else if (objName.Contains("right"))
            {
                rotationDirection = 1f;
            }
        }
    }

    /// <summary>
    /// Called by GazeHoverTrigger when gaze enters
    /// </summary>
    public void OnGazeEnter()
    {
        if (rotationScript != null)
        {
            rotationScript.OnMouseHoverEnter(rotationDirection);
        }
    }

    /// <summary>
    /// Called by GazeHoverTrigger when gaze exits
    /// </summary>
    public void OnGazeExit()
    {
        if (rotationScript != null)
        {
            rotationScript.OnMouseHoverExit();
        }
    }

    /// <summary>
    /// Set rotation direction at runtime
    /// </summary>
    public void SetDirection(float direction)
    {
        rotationDirection = direction;
    }
}
