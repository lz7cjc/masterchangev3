using UnityEngine;
using UnityEngine.SceneManagement;

// This adapter script allows connecting the existing showfilm class with the new VR tipping system
// without modifying your original showfilm class
public class ShowfilmAdapter : MonoBehaviour
{
    public GameObject tipsPanel;
    public IntegratedTipsController tipsController;

    // Reference to the existing showfilm component
    private showfilm existingShowfilm;

    // Keep track of whether we're already tipping
    private bool tippingActive = false;

    void Start()
    {
        // Get reference to the existing showfilm component
        existingShowfilm = GetComponent<showfilm>();

        if (existingShowfilm == null)
        {
            existingShowfilm = FindObjectOfType<showfilm>();
            if (existingShowfilm == null)
            {
                Debug.LogError("ShowfilmAdapter: Could not find showfilm component!");
            }
        }

        // Find tipsController if not set
        if (tipsController == null && tipsPanel != null)
        {
            tipsController = tipsPanel.GetComponent<IntegratedTipsController>();
        }
        else if (tipsController == null)
        {
            tipsController = FindObjectOfType<IntegratedTipsController>();
        }

        // Initially hide tips panel if available
        if (tipsPanel != null)
        {
            tipsPanel.SetActive(false);
        }
    }

    // This method should be called by MergedVideoController.OnVideoFinished
    // It will then call the original showfilm.tipping() and enhance it
    public void EnhancedTipping()
    {
        Debug.Log("ShowfilmAdapter: EnhancedTipping called");

        if (tippingActive)
            return;

        tippingActive = true;

        // Call the original tipping method to maintain backward compatibility
        if (existingShowfilm != null)
        {
            existingShowfilm.tipping();
        }

        // Add VR enhancements
        if (tipsController != null)
        {
            tipsController.ShowTipsPanel();
        }
        else if (tipsPanel != null)
        {
            // Fallback if tipsController not available
            PositionTipsPanelInFrontOfCamera();
            tipsPanel.SetActive(true);
        }
    }

    // Position the tips panel in front of the camera
    private void PositionTipsPanelInFrontOfCamera()
    {
        if (tipsPanel != null && Camera.main != null)
        {
            Vector3 panelPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1.5f);
            tipsPanel.transform.position = panelPosition;

            // Ensure the panel faces the camera
            tipsPanel.transform.LookAt(2 * tipsPanel.transform.position - Camera.main.transform.position);
        }
    }

    // Call this to hide the tips panel
    public void CloseTipping()
    {
        tippingActive = false;

        if (tipsController != null)
        {
            tipsController.HideTipsPanel();
        }
        else if (tipsPanel != null)
        {
            tipsPanel.SetActive(false);
        }
    }
}