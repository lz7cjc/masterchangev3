using UnityEngine;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates a curved canvas for VR UI elements with proper text orientation
/// and layout that works reliably in VR.
/// </summary>
public class SimpleCurvedCanvas : MonoBehaviour
{
    [Header("Curve Settings")]
    [Range(1f, 5f)]
    public float radius = 2.5f;

    [Range(10f, 180f)]
    public float curveAngle = 60f;

    [Header("Positioning")]
    public float distanceFromCamera = 2.0f;
    public float verticalOffset = 0f;

    [Header("Button Layout")]
    public GameObject[] tipButtons;
    public bool useGridLayout = true;
    public int buttonsPerRow = 3;
    public float rowHeight = 0.4f;
    public float buttonsY = 0f;

    [Header("Title Elements")]
    public GameObject titlePanel;
    public float titleY = 0.4f;

    [Header("Error Message")]
    public GameObject errorPanel;
    public GameObject errorButton;
    [SerializeField] private TMP_Text errorText;
    public float errorY = -0.4f;

    [Header("Camera Reference")]
    public Transform mainCamera;

    [Header("Position Management")]
    public bool savePositions = false;
    public bool loadSavedPositions = false;
    public bool applyPositionsOnStart = true;

    // Dictionary to store saved positions
    private Dictionary<string, Vector3> savedPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> savedRotations = new Dictionary<string, Quaternion>();

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Hide error message by default
        if (errorPanel != null)
            errorPanel.SetActive(false);

        if (applyPositionsOnStart)
        {
            // Apply positions based on settings
            if (loadSavedPositions && HasSavedPositions())
            {
                LoadElementPositions();
            }
            else
            {
                ArrangeAllElements();
            }
        }
    }

    // Check if we have saved positions
    private bool HasSavedPositions()
    {
        return savedPositions.Count > 0;
    }

    // Arrange all UI elements (title, buttons, error panel)
    public void ArrangeAllElements()
    {
        // Place the panel in front of the camera first
        PositionInFrontOfCamera();

        // Clear saved positions when generating new arrangement
        savedPositions.Clear();
        savedRotations.Clear();

        // Position title panel on the curve
        PositionTitlePanel();

        // Position buttons
        ArrangeButtons();

        // Position error panel
        PositionErrorPanel();

        // Fix text orientation in all elements
        FixTextOrientation();

        // Save positions after arranging if needed
        if (savePositions)
        {
            SaveElementPositions();
        }
    }

    // Fix text orientation in all UI elements
    private void FixTextOrientation()
    {
        // Fix title text
        if (titlePanel != null)
        {
            FixTextInObject(titlePanel);
        }

        // Fix button texts
        if (tipButtons != null)
        {
            foreach (GameObject button in tipButtons)
            {
                if (button != null)
                    FixTextInObject(button);
            }
        }

        // Fix error text
        if (errorPanel != null)
        {
            FixTextInObject(errorPanel);
        }
    }

    // Fix text orientation in a specific object
    private void FixTextInObject(GameObject obj)
    {
        // Find all TextMeshPro components in children
        TMP_Text[] texts = obj.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;

            // Ensure text is visible in front of parent
            text.transform.localPosition = new Vector3(
                text.transform.localPosition.x,
                text.transform.localPosition.y,
                -0.01f
            );

            // Reset local rotation to face forward
            text.transform.localRotation = Quaternion.identity;
        }
    }

    // Save current positions of all elements
    public void SaveElementPositions()
    {
        savedPositions.Clear();
        savedRotations.Clear();

        // Save title panel position
        if (titlePanel != null)
        {
            savedPositions.Add(titlePanel.name, titlePanel.transform.localPosition);
            savedRotations.Add(titlePanel.name, titlePanel.transform.localRotation);
            Debug.Log("Saved position for: " + titlePanel.name);
        }

        // Save button positions
        if (tipButtons != null)
        {
            for (int i = 0; i < tipButtons.Length; i++)
            {
                if (tipButtons[i] != null)
                {
                    savedPositions.Add(tipButtons[i].name, tipButtons[i].transform.localPosition);
                    savedRotations.Add(tipButtons[i].name, tipButtons[i].transform.localRotation);
                    Debug.Log("Saved position for: " + tipButtons[i].name);
                }
            }
        }

        // Save error panel position
        if (errorPanel != null)
        {
            savedPositions.Add(errorPanel.name, errorPanel.transform.localPosition);
            savedRotations.Add(errorPanel.name, errorPanel.transform.localRotation);
            Debug.Log("Saved position for: " + errorPanel.name);
        }

        Debug.Log("Saved positions for " + savedPositions.Count + " elements");
    }

    // Load saved positions for all elements
    public void LoadElementPositions()
    {
        if (savedPositions.Count == 0)
        {
            Debug.LogWarning("No saved positions to load");
            return;
        }

        // Load title panel position
        if (titlePanel != null && savedPositions.ContainsKey(titlePanel.name))
        {
            titlePanel.transform.localPosition = savedPositions[titlePanel.name];
            titlePanel.transform.localRotation = savedRotations[titlePanel.name];
        }

        // Load button positions
        if (tipButtons != null)
        {
            for (int i = 0; i < tipButtons.Length; i++)
            {
                if (tipButtons[i] != null && savedPositions.ContainsKey(tipButtons[i].name))
                {
                    tipButtons[i].transform.localPosition = savedPositions[tipButtons[i].name];
                    tipButtons[i].transform.localRotation = savedRotations[tipButtons[i].name];
                }
            }
        }

        // Load error panel position
        if (errorPanel != null && savedPositions.ContainsKey(errorPanel.name))
        {
            errorPanel.transform.localPosition = savedPositions[errorPanel.name];
            errorPanel.transform.localRotation = savedRotations[errorPanel.name];
        }

        Debug.Log("Loaded saved positions");
    }

    // Position the title panel at the top of the curved layout
    private void PositionTitlePanel()
    {
        if (titlePanel == null)
            return;

        // Place the title at the top-center of the curve
        float y = titleY;
        float z = -radius;

        titlePanel.transform.localPosition = new Vector3(0, y, z);
        titlePanel.transform.localRotation = Quaternion.identity;

        // Ensure it has a collider if needed
        EnsureHasCollider(titlePanel);
    }

    // Position error panel at the bottom
    private void PositionErrorPanel()
    {
        if (errorPanel == null)
            return;

        // Place the error at the bottom-center
        float y = errorY;
        float z = -radius;

        errorPanel.transform.localPosition = new Vector3(0, y, z);
        errorPanel.transform.localRotation = Quaternion.identity;

        // Ensure error button has a collider
        if (errorButton != null)
            EnsureHasCollider(errorButton);
    }

    // Arrange buttons in a curved layout
    private void ArrangeButtons()
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
            tipButtons[i].transform.localPosition = new Vector3(x, buttonsY, z);
            tipButtons[i].transform.localRotation = Quaternion.Euler(0, angle, 0);

            // Add a BoxCollider if needed
            EnsureHasCollider(tipButtons[i]);
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

            // Use a fixed curve angle for each row
            float rowAngle = curveAngle;

            // Calculate the angle step based on max buttons per row
            float angleStep = rowAngle / (buttonsPerRow - 1);
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
                float y = buttonsY + ((rows - 1) / 2f - row) * rowHeight;
                float z = Mathf.Cos(angleRadians) * radius - radius;

                // Set position and rotation to face center
                tipButtons[index].transform.localPosition = new Vector3(x, y, z);
                tipButtons[index].transform.localRotation = Quaternion.Euler(0, angle, 0);

                // Add a BoxCollider if needed
                EnsureHasCollider(tipButtons[index]);
            }
        }
    }

    // Ensure the GameObject has a collider for interactions
    private void EnsureHasCollider(GameObject obj)
    {
        BoxCollider collider = obj.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Try to size the collider based on visual elements
            RectTransform rect = obj.GetComponent<RectTransform>();
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
                Renderer renderer = obj.GetComponent<Renderer>();
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
    public void PositionInFrontOfCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Move to camera position + forward * distance
        transform.position = mainCamera.position + mainCamera.forward * distanceFromCamera +
                                                   mainCamera.up * verticalOffset;

        // Match camera rotation on Y axis only (keep vertical orientation)
        Vector3 eulerAngles = mainCamera.eulerAngles;
        transform.eulerAngles = new Vector3(0, eulerAngles.y, 0);
    }

    // Show the error message
    public void ShowError(string message)
    {
        if (errorPanel == null)
            return;

        if (errorText != null)
            errorText.text = message;

        errorPanel.SetActive(true);
    }

    // Hide the error message
    public void HideError()
    {
        if (errorPanel == null)
            return;

        errorPanel.SetActive(false);
    }

    // Show the entire tips panel
    public void ShowTipsPanel()
    {
        gameObject.SetActive(true);
        PositionInFrontOfCamera();

        // Apply positions based on settings
        if (loadSavedPositions && HasSavedPositions())
        {
            LoadElementPositions();
        }
        else
        {
            ArrangeAllElements();
        }

        HideError(); // Hide error message by default
    }

    // Hide the tips panel
    public void HideTipsPanel()
    {
        gameObject.SetActive(false);
    }

    // Unity Editor methods - for previewing layout during design
    void OnValidate()
    {
        // Auto-update in editor when values change
        if (Application.isEditor && !Application.isPlaying)
        {
            // If we're loading saved positions, don't auto-arrange
            if (!loadSavedPositions || !HasSavedPositions())
            {
                ArrangeAllElements();
            }
        }
    }

    // Export positions to a string format that can be saved
    public string ExportPositions()
    {
        if (savedPositions.Count == 0)
        {
            SaveElementPositions();
        }

        string output = "Saved Element Positions:\n";
        foreach (var item in savedPositions)
        {
            Vector3 pos = item.Value;
            Quaternion rot = savedRotations[item.Key];
            output += $"{item.Key}: Position({pos.x:F3}, {pos.y:F3}, {pos.z:F3}) Rotation({rot.eulerAngles.x:F1}, {rot.eulerAngles.y:F1}, {rot.eulerAngles.z:F1})\n";
        }

        Debug.Log(output);
        return output;
    }

#if UNITY_EDITOR
    // Editor utility methods
    [CustomEditor(typeof(SimpleCurvedCanvas))]
    public class SimpleCurvedCanvasEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SimpleCurvedCanvas layout = (SimpleCurvedCanvas)target;

            GUILayout.Space(10);
            GUILayout.Label("Layout Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Curved Layout"))
            {
                layout.ArrangeAllElements();
            }

            if (GUILayout.Button("Fix Text Orientation"))
            {
                layout.FixTextOrientation();
            }

            if (GUILayout.Button("Save Current Positions"))
            {
                layout.SaveElementPositions();
            }

            if (GUILayout.Button("Load Saved Positions"))
            {
                layout.LoadElementPositions();
            }

            if (GUILayout.Button("Export Position Data"))
            {
                layout.ExportPositions();
            }

            if (GUILayout.Button("Position In Front of Camera"))
            {
                layout.PositionInFrontOfCamera();
            }
        }
    }
#endif
}