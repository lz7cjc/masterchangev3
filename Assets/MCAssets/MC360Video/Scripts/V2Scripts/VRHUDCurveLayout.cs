using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arranges HUD buttons in a curved arc that bends TOWARD the camera.
/// Attach to the panel containing your HUD buttons.
/// Each button is equidistant from the player's viewpoint.
/// </summary>
public class VRHUDCurveLayout : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float curveRadius = 1.5f; // Distance from center
    [SerializeField] private float totalArcAngle = 90f; // Total arc spread (degrees)
    [SerializeField] private float curveDepth = 0.3f; // How much to curve toward camera (NEGATIVE Z)

    [Header("Button Setup")]
    [SerializeField] private RectTransform[] buttons; // Assign in order left to right
    [SerializeField] private bool autoFindButtons = true;

    [Header("Options")]
    [SerializeField] private bool updateInEditor = true;
    [SerializeField] private bool updateAtRuntime = false;
    [SerializeField] private bool rotateButtonsToFaceCenter = true;

    void Start()
    {
        if (autoFindButtons || buttons == null || buttons.Length == 0)
        {
            FindButtons();
        }

        LayoutButtons();
    }

    void Update()
    {
        if (updateAtRuntime)
        {
            LayoutButtons();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (updateInEditor && Application.isPlaying == false)
        {
            if (autoFindButtons || buttons == null || buttons.Length == 0)
            {
                FindButtons();
            }
            LayoutButtons();
        }
    }
#endif

    private void FindButtons()
    {
        // Get all RectTransforms that are direct children
        int childCount = transform.childCount;
        buttons = new RectTransform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            buttons[i] = transform.GetChild(i).GetComponent<RectTransform>();
        }

        Debug.Log($"VRHUDCurveLayout: Found {buttons.Length} buttons");
    }

    [ContextMenu("Layout Buttons")]
    private void LayoutButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("VRHUDCurveLayout: No buttons assigned!");
            return;
        }

        int buttonCount = buttons.Length;
        float angleStep = totalArcAngle / Mathf.Max(1, buttonCount - 1);
        float startAngle = -totalArcAngle / 2f; // Center the arc

        for (int i = 0; i < buttonCount; i++)
        {
            if (buttons[i] == null) continue;

            // Calculate angle for this button
            float angle = startAngle + (angleStep * i);
            float angleRad = angle * Mathf.Deg2Rad;

            // Position on horizontal arc
            float x = Mathf.Sin(angleRad) * curveRadius;
            float y = 0; // Keep all buttons at same height

            // KEY FIX: Negative Z moves TOWARD camera (concave curve)
            // Cosine gives us the forward distance - more curve at edges
            float z = -Mathf.Abs(Mathf.Cos(angleRad)) * curveDepth;

            Vector3 position = new Vector3(x, y, z);
            buttons[i].localPosition = position;

            // Rotate button to face the camera/player
            if (rotateButtonsToFaceCenter)
            {
                // Each button angles inward to face player
                Quaternion rotation = Quaternion.Euler(0, -angle, 0);
                buttons[i].localRotation = rotation;
            }
        }

        Debug.Log($"VRHUDCurveLayout: Positioned {buttonCount} buttons in curved arc");
    }

    /// <summary>
    /// Adjust curve dynamically
    /// </summary>
    public void SetCurveRadius(float radius)
    {
        curveRadius = radius;
        LayoutButtons();
    }

    public void SetCurveDepth(float depth)
    {
        curveDepth = depth;
        LayoutButtons();
    }

    public void SetArcAngle(float angle)
    {
        totalArcAngle = angle;
        LayoutButtons();
    }
}