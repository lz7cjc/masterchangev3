using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntegratedTipsController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tipsPanel;
    public TMP_Text balanceText;
    public TMP_Text errorMessageText;
    public GameObject problemButton;

    [Header("VR Settings")]
    public Transform mainCamera;
    public float distanceFromCamera = 1.5f;
    public bool faceCamera = true;
    public float smoothTime = 0.3f;
    public bool flipCanvasHorizontally = false; // Add this if tips appear backwards

    [Header("Visual Feedback")]
    public Color defaultColor = Color.white;
    public Color hoverColor = new Color(0.6f, 1f, 0.6f); // Light green
    public Color selectedColor = new Color(1f, 1f, 0.6f); // Light yellow
    public Image gazeProgressIndicator; // Optional - for visual feedback

    [Header("Debug")]
    public bool showDebugInfo = false;

    private Vector3 velocity = Vector3.zero;
    private bool isActive = false;
    private bool isPositioning = false;

    private Image[] tipButtons;
    private Color[] originalColors;

    void Start()
    {
        // Get camera reference if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Initially hide the panel if not already hidden
        if (tipsPanel != null)
            tipsPanel.SetActive(false);

        // Store original colors of tip buttons if available
        if (tipsPanel != null)
        {
            tipButtons = tipsPanel.GetComponentsInChildren<Image>();
            originalColors = new Color[tipButtons.Length];
            for (int i = 0; i < tipButtons.Length; i++)
            {
                if (tipButtons[i] != null)
                    originalColors[i] = tipButtons[i].color;
            }
        }

        // Hide gaze progress indicator if available
        if (gazeProgressIndicator != null)
            gazeProgressIndicator.gameObject.SetActive(false);

        // Update balance text if available
        UpdateBalanceText();

        if (showDebugInfo)
            Debug.Log("IntegratedTipsController: Initialized");
    }

    void Update()
    {
        if (!isActive || mainCamera == null)
            return;

        if (isPositioning)
        {
            // Position the panel in front of the camera at the specified distance
            Vector3 targetPosition = mainCamera.position + (mainCamera.forward * distanceFromCamera);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            // Make the panel match the camera's rotation exactly
            transform.rotation = mainCamera.rotation;

            // Apply horizontal flip if needed (for backwards text)
            if (flipCanvasHorizontally)
            {
                // Rotate 180 degrees around the up axis
                transform.Rotate(0, 180, 0);
            }
        }
    }

    // Called when the video ends
    public void ShowTipsPanel()
    {
        if (tipsPanel != null)
        {
            tipsPanel.SetActive(true);
            isActive = true;
            isPositioning = true;

            // Position it immediately in front of the camera to start
            if (mainCamera != null)
            {
                transform.position = mainCamera.position + (mainCamera.forward * distanceFromCamera);

                // Set rotation to match camera's rotation exactly
                transform.rotation = mainCamera.rotation;

                // Apply horizontal flip if needed (for backwards text)
                if (flipCanvasHorizontally)
                {
                    // Rotate 180 degrees around the up axis
                    transform.Rotate(0, 180, 0);
                }
            }

            // Update balance display
            UpdateBalanceText();

            if (showDebugInfo)
                Debug.Log("IntegratedTipsController: Tips panel shown at position " + transform.position);
        }
        else
        {
            Debug.LogError("Tips panel reference not set!");
        }
    }

    // Call this to hide the panel
    public void HideTipsPanel()
    {
        isActive = false;
        isPositioning = false;
        if (tipsPanel != null)
            tipsPanel.SetActive(false);

        if (showDebugInfo)
            Debug.Log("IntegratedTipsController: Tips panel hidden");
    }

    // Update the balance text with current Riros balance
    private void UpdateBalanceText()
    {
        if (balanceText != null && PlayerPrefs.HasKey("rirosBalance"))
        {
            int balance = PlayerPrefs.GetInt("rirosBalance");
            balanceText.text = "Your Balance is: R$" + balance.ToString();
        }
    }

    // Called by Event Trigger when pointer enters button
    public void OnPointerEnter(GameObject button)
    {
        if (button == null)
            return;

        if (showDebugInfo)
            Debug.Log("Pointer entered: " + button.name);

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = hoverColor;
        }

        // If we have a gaze progress indicator, show it and position it near the button
        if (gazeProgressIndicator != null)
        {
            gazeProgressIndicator.gameObject.SetActive(true);
            gazeProgressIndicator.fillAmount = 0f;

            // Position it near the button if appropriate
            RectTransform indicatorRect = gazeProgressIndicator.GetComponent<RectTransform>();
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (indicatorRect != null && buttonRect != null)
            {
                indicatorRect.position = buttonRect.position;
            }
        }
    }

    // Called by Event Trigger when pointer exits button
    public void OnPointerExit(GameObject button)
    {
        if (button == null)
            return;

        if (showDebugInfo)
            Debug.Log("Pointer exited: " + button.name);

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = defaultColor;
        }

        // Hide the gaze progress indicator if available
        if (gazeProgressIndicator != null)
        {
            gazeProgressIndicator.fillAmount = 0f;
            gazeProgressIndicator.gameObject.SetActive(false);
        }
    }

    // Use this method to handle errors from the tips system
    public void ShowError(string errorMsg)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = errorMsg;

            if (problemButton != null)
                problemButton.SetActive(true);

            if (showDebugInfo)
                Debug.Log("IntegratedTipsController: Showing error: " + errorMsg);
        }
    }

    // Call this to reposition the panel if the user has moved
    public void UpdatePosition()
    {
        if (!isActive || mainCamera == null)
            return;

        Vector3 targetPosition = mainCamera.position + (mainCamera.forward * distanceFromCamera);
        transform.position = targetPosition;

        // Set rotation to match camera's rotation exactly
        transform.rotation = mainCamera.rotation;

        // Apply horizontal flip if needed (for backwards text)
        if (flipCanvasHorizontally)
        {
            // Rotate 180 degrees around the up axis
            transform.Rotate(0, 180, 0);
        }

        if (showDebugInfo)
            Debug.Log("IntegratedTipsController: Panel position updated");
    }
}