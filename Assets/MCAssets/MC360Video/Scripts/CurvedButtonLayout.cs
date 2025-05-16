using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A dead-simple script to arrange buttons in a curved layout for VR.
/// This version works with TextMeshPro and existing button objects.
/// </summary>
public class CurvedButtonLayout : MonoBehaviour
{
    [Header("Curve Settings")]
    [Range(1f, 5f)]
    public float radius = 2.0f;

    [Range(0f, 180f)]
    public float curveAngle = 60f;

    [Header("Button References")]
    public GameObject[] tipButtons;

    [Header("Layout Type")]
    public bool useGridLayout = true;
    public int buttonsPerRow = 3;
    public float rowHeight = 0.3f;

    [Header("Camera Reference")]
    public Transform mainCamera;

    // Call this to update the button layout
    public void ArrangeButtons()
    {
        if (tipButtons == null || tipButtons.Length == 0)
            return;

        if (useGridLayout)
            ArrangeButtonsInGrid();
        else
            ArrangeButtonsInArc();
    }

    // Arrange buttons in a curved arc
    private void ArrangeButtonsInArc()
    {
        int buttonCount = tipButtons.Length;
        float angleStep = curveAngle / buttonCount;
        float startAngle = -curveAngle / 2;

        for (int i = 0; i < buttonCount; i++)
        {
            if (tipButtons[i] == null)
                continue;

            // Calculate angle and position on the curve
            float angle = startAngle + (i * angleStep);
            float angleRadians = angle * Mathf.Deg2Rad;

            // Position on curve (x = sin, z = cos-1)
            float x = Mathf.Sin(angleRadians) * radius;
            float z = Mathf.Cos(angleRadians) * radius - radius;

            // Set position and rotation to face center
            tipButtons[i].transform.localPosition = new Vector3(x, 0, z);
            tipButtons[i].transform.localRotation = Quaternion.Euler(0, angle, 0);

            // Add a BoxCollider if needed
            EnsureButtonHasCollider(tipButtons[i]);
        }
    }

    // Arrange buttons in a curved grid layout
    private void ArrangeButtonsInGrid()
    {
        int buttonCount = tipButtons.Length;
        int rows = (buttonCount + buttonsPerRow - 1) / buttonsPerRow;

        for (int row = 0; row < rows; row++)
        {
            int itemsInThisRow = Mathf.Min(buttonsPerRow, buttonCount - row * buttonsPerRow);
            float rowAngle = curveAngle * ((float)itemsInThisRow / buttonsPerRow);
            float angleStep = rowAngle / itemsInThisRow;
            float startAngle = -rowAngle / 2;

            for (int col = 0; col < itemsInThisRow; col++)
            {
                int index = row * buttonsPerRow + col;
                if (index >= buttonCount || tipButtons[index] == null)
                    continue;

                // Calculate angle and position on the curve
                float angle = startAngle + (col * angleStep);
                float angleRadians = angle * Mathf.Deg2Rad;

                // Position on curve with row height offset
                float x = Mathf.Sin(angleRadians) * radius;
                float y = (rows / 2f - row) * rowHeight;
                float z = Mathf.Cos(angleRadians) * radius - radius;

                // Set position and rotation to face center
                tipButtons[index].transform.localPosition = new Vector3(x, y, z);
                tipButtons[index].transform.localRotation = Quaternion.Euler(0, angle, 0);

                // Add a BoxCollider if needed
                EnsureButtonHasCollider(tipButtons[index]);
            }
        }
    }

    // Ensure the button has a collider for interactions
    private void EnsureButtonHasCollider(GameObject button)
    {
        BoxCollider collider = button.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = button.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Try to size the collider based on visual elements
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Size based on RectTransform for UI elements
                collider.size = new Vector3(
                    rect.rect.width / 1000f,
                    rect.rect.height / 1000f,
                    0.01f
                );
            }
            else
            {
                // Default size for 3D objects
                Renderer renderer = button.GetComponent<Renderer>();
                if (renderer != null)
                {
                    collider.size = renderer.bounds.size;
                }
                else
                {
                    // Fallback size
                    collider.size = new Vector3(0.2f, 0.1f, 0.01f);
                }
            }
        }
    }

    // Position the entire curved layout in front of the camera
    public void PositionInFrontOfCamera(float distance = 2.0f)
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Move to camera position + forward * distance
        transform.position = mainCamera.position + mainCamera.forward * distance;

        // Match camera rotation on Y axis only (keep vertical orientation)
        Vector3 eulerAngles = mainCamera.eulerAngles;
        transform.eulerAngles = new Vector3(0, eulerAngles.y, 0);
    }

    // Useful for calling from other scripts
    public void ShowTipsPanel()
    {
        gameObject.SetActive(true);
        PositionInFrontOfCamera(radius);
        ArrangeButtons();
    }

    // Unity Editor methods - for previewing layout during design
    void OnValidate()
    {
        // Auto-update in editor when values change
        if (Application.isEditor && !Application.isPlaying)
        {
            ArrangeButtons();
        }
    }

    // Optional: Add this for initial setup
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        ArrangeButtons();
    }
}