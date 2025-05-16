using UnityEngine;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A simple, reliable curved canvas for VR.
/// </summary>
public class BasicCurvedCanvas : MonoBehaviour
{
    [Header("Curve Settings")]
    [Range(1f, 5f)]
    public float radius = 2.5f;

    [Range(10f, 90f)]
    public float curveAngle = 60f;

    [Header("Positioning")]
    public float distanceFromCamera = 2.0f;
    public float verticalOffset = 0f;

    [Header("Panel References")]
    public GameObject titlePanel;
    public GameObject buttonsPanel;
    public GameObject errorPanel;

    [Header("Button Layout")]
    public GameObject[] tipButtons;
    public bool useGridLayout = true;
    public int buttonsPerRow = 3;
    public float rowHeight = 0.3f;

    [Header("Vertical Positioning")]
    public float titleY = 0.4f;
    public float buttonsY = 0f;
    public float errorY = -0.4f;

    [Header("Error Message")]
    public TMP_Text errorText;

    [Header("Camera Reference")]
    public Transform mainCamera;

    // Dictionary to store saved positions
    private Dictionary<string, Vector3> savedPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> savedRotations = new Dictionary<string, Quaternion>();

    // Static dictionaries to persist between play sessions
    private static Dictionary<string, Vector3> persistentPositions = new Dictionary<string, Vector3>();
    private static Dictionary<string, Quaternion> persistentRotations = new Dictionary<string, Quaternion>();

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Hide error by default
        if (errorPanel != null)
            errorPanel.SetActive(false);

        // Load saved positions if available
        if (persistentPositions.Count > 0)
        {
            LoadPositions();
        }
        else
        {
            ArrangeCanvas();
        }
    }

    /// <summary>
    /// Arrange the entire canvas in front of the camera with a curved layout
    /// </summary>
    public void ArrangeCanvas()
    {
        // First position the canvas in front of the camera
        PositionCanvasInFrontOfCamera();

        // Position panels
        PositionMainPanels();

        // Position buttons
        if (useGridLayout)
            ArrangeButtonsInGrid();
        else
            ArrangeButtonsInRow();

        // Fix text orientation
        FixTextOrientation();

        // Save positions
        SavePositions();
    }

    /// <summary>
    /// Position the canvas in front of the camera
    /// </summary>
    public void PositionCanvasInFrontOfCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Position canvas in front of camera
        transform.position = mainCamera.position + mainCamera.forward * distanceFromCamera +
                                                  mainCamera.up * verticalOffset;

        // Match rotation on Y axis only
        Vector3 eulerAngles = mainCamera.eulerAngles;
        transform.eulerAngles = new Vector3(0, eulerAngles.y, 0);
    }

    /// <summary>
    /// Position the main panels (title, buttons, error)
    /// </summary>
    private void PositionMainPanels()
    {
        // Position title panel
        if (titlePanel != null)
        {
            titlePanel.transform.localPosition = new Vector3(0, titleY, 0);
            titlePanel.transform.localRotation = Quaternion.identity;
        }

        // Position buttons panel
        if (buttonsPanel != null)
        {
            buttonsPanel.transform.localPosition = new Vector3(0, buttonsY, 0);
            buttonsPanel.transform.localRotation = Quaternion.identity;
        }

        // Position error panel
        if (errorPanel != null)
        {
            errorPanel.transform.localPosition = new Vector3(0, errorY, 0);
            errorPanel.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Arrange buttons in a single curved row
    /// </summary>
    private void ArrangeButtonsInRow()
    {
        if (tipButtons == null || tipButtons.Length == 0)
            return;

        int buttonCount = tipButtons.Length;
        float angleStep = curveAngle / buttonCount;
        float startAngle = -curveAngle / 2;

        for (int i = 0; i < buttonCount; i++)
        {
            if (tipButtons[i] == null)
                continue;

            // Calculate position on curve
            float angle = startAngle + (i * angleStep);
            float angleRadians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(angleRadians) * radius;
            float z = Mathf.Cos(angleRadians) * radius - radius;

            // Position button
            tipButtons[i].transform.localPosition = new Vector3(x, 0, z);

            // Rotate to face center
            tipButtons[i].transform.localRotation = Quaternion.Euler(0, angle, 0);

            // Ensure it has a collider
            EnsureHasCollider(tipButtons[i]);
        }
    }

    /// <summary>
    /// Arrange buttons in a curved grid
    /// </summary>
    private void ArrangeButtonsInGrid()
    {
        if (tipButtons == null || tipButtons.Length == 0)
            return;

        int buttonCount = tipButtons.Length;
        int rows = (buttonCount + buttonsPerRow - 1) / buttonsPerRow;

        for (int row = 0; row < rows; row++)
        {
            int itemsInThisRow = Mathf.Min(buttonsPerRow, buttonCount - row * buttonsPerRow);
            float rowAngle = curveAngle;
            float angleStep = rowAngle / (buttonsPerRow - 1);
            float startAngle = -rowAngle / 2;

            for (int col = 0; col < itemsInThisRow; col++)
            {
                int index = row * buttonsPerRow + col;
                if (index >= buttonCount || tipButtons[index] == null)
                    continue;

                // Calculate position on curve
                float angle = startAngle + (col * angleStep);
                float angleRadians = angle * Mathf.Deg2Rad;

                float x = Mathf.Sin(angleRadians) * radius;
                float y = ((rows - 1) / 2f - row) * rowHeight;
                float z = Mathf.Cos(angleRadians) * radius - radius;

                // Position button
                tipButtons[index].transform.localPosition = new Vector3(x, y, z);

                // Rotate to face center
                tipButtons[index].transform.localRotation = Quaternion.Euler(0, angle, 0);

                // Ensure it has a collider
                EnsureHasCollider(tipButtons[index]);
            }
        }
    }

    /// <summary>
    /// Fix orientation of text components
    /// </summary>
    public void FixTextOrientation()
    {
        // Fix text in entire canvas
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            if (text == null)
                continue;

            // Move text slightly forward
            text.transform.localPosition = new Vector3(
                text.transform.localPosition.x,
                text.transform.localPosition.y,
                -0.01f
            );

            // Reset rotation
            text.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Ensure object has a collider for interaction
    /// </summary>
    private void EnsureHasCollider(GameObject obj)
    {
        BoxCollider collider = obj.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Size based on renderers
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                collider.size = renderer.bounds.size;
            }
            else
            {
                // Default size
                collider.size = new Vector3(0.2f, 0.1f, 0.01f);
            }
        }
    }

    /// <summary>
    /// Save positions for all elements
    /// </summary>
    public void SavePositions()
    {
        savedPositions.Clear();
        savedRotations.Clear();

        // Save panel positions
        SaveObjectPositionAndRotation(titlePanel);
        SaveObjectPositionAndRotation(buttonsPanel);
        SaveObjectPositionAndRotation(errorPanel);

        // Save button positions
        if (tipButtons != null)
        {
            foreach (GameObject button in tipButtons)
            {
                SaveObjectPositionAndRotation(button);
            }
        }

        // Copy to static dictionaries for persistence
        persistentPositions.Clear();
        persistentRotations.Clear();

        foreach (var kvp in savedPositions)
            persistentPositions[kvp.Key] = kvp.Value;

        foreach (var kvp in savedRotations)
            persistentRotations[kvp.Key] = kvp.Value;

        Debug.Log("Saved positions for " + savedPositions.Count + " objects");
    }

    /// <summary>
    /// Save position and rotation for a specific object
    /// </summary>
    private void SaveObjectPositionAndRotation(GameObject obj)
    {
        if (obj == null)
            return;

        savedPositions[obj.name] = obj.transform.localPosition;
        savedRotations[obj.name] = obj.transform.localRotation;
    }

    /// <summary>
    /// Load saved positions
    /// </summary>
    public void LoadPositions()
    {
        if (persistentPositions.Count == 0)
            return;

        // Copy from persistent storage
        savedPositions = new Dictionary<string, Vector3>(persistentPositions);
        savedRotations = new Dictionary<string, Quaternion>(persistentRotations);

        // Load panel positions
        LoadObjectPositionAndRotation(titlePanel);
        LoadObjectPositionAndRotation(buttonsPanel);
        LoadObjectPositionAndRotation(errorPanel);

        // Load button positions
        if (tipButtons != null)
        {
            foreach (GameObject button in tipButtons)
            {
                LoadObjectPositionAndRotation(button);
            }
        }

        // Fix text orientation
        FixTextOrientation();

        Debug.Log("Loaded positions for " + savedPositions.Count + " objects");
    }

    /// <summary>
    /// Load position and rotation for a specific object
    /// </summary>
    private void LoadObjectPositionAndRotation(GameObject obj)
    {
        if (obj == null || !savedPositions.ContainsKey(obj.name))
            return;

        obj.transform.localPosition = savedPositions[obj.name];
        obj.transform.localRotation = savedRotations[obj.name];
    }

    /// <summary>
    /// Reset all saved positions
    /// </summary>
    public void ResetPositions()
    {
        savedPositions.Clear();
        savedRotations.Clear();
        persistentPositions.Clear();
        persistentRotations.Clear();

        ArrangeCanvas();
    }

    /// <summary>
    /// Show error message
    /// </summary>
    public void ShowError(string message)
    {
        if (errorPanel == null)
            return;

        if (errorText != null)
            errorText.text = message;

        errorPanel.SetActive(true);
    }

    /// <summary>
    /// Hide error message
    /// </summary>
    public void HideError()
    {
        if (errorPanel == null)
            return;

        errorPanel.SetActive(false);
    }

    /// <summary>
    /// Show the canvas
    /// </summary>
    public void ShowCanvas()
    {
        gameObject.SetActive(true);
        PositionCanvasInFrontOfCamera();
    }

    /// <summary>
    /// Hide the canvas
    /// </summary>
    public void HideCanvas()
    {
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BasicCurvedCanvas))]
    public class BasicCurvedCanvasEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BasicCurvedCanvas canvas = (BasicCurvedCanvas)target;

            GUILayout.Space(10);
            GUILayout.Label("Layout Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Arrange Canvas"))
            {
                canvas.ArrangeCanvas();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Fix Text Orientation"))
            {
                canvas.FixTextOrientation();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Position In Front of Camera"))
            {
                canvas.PositionCanvasInFrontOfCamera();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save Positions"))
            {
                canvas.SavePositions();
            }

            if (GUILayout.Button("Load Positions"))
            {
                canvas.LoadPositions();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Reset Positions"))
            {
                canvas.ResetPositions();
                SceneView.RepaintAll();
            }
        }
    }
#endif
}