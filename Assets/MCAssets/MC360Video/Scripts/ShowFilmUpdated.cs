using UnityEngine;
using UnityEngine.SceneManagement;

public class ShowfilmUpdated : MonoBehaviour
{
    [Header("VR Tipping System")]
    public GameObject tipsPanel;
    public IntegratedTipsController tipsController;
    public UpdatedFilmvotes2 filmVotes;

    [Header("Video Player")]
    public MergedVideoController videoController;

    private bool tippingActive = false;

    void Start()
    {
        // Find references if not set in inspector
        if (tipsController == null && tipsPanel != null)
        {
            tipsController = tipsPanel.GetComponent<IntegratedTipsController>();
        }

        if (filmVotes == null)
        {
            filmVotes = GetComponent<UpdatedFilmvotes2>();
            if (filmVotes == null)
            {
                filmVotes = FindObjectOfType<UpdatedFilmvotes2>();
            }
        }

        if (videoController == null)
        {
            videoController = FindObjectOfType<MergedVideoController>();
        }

        // Initially hide tips panel
        if (tipsPanel != null)
        {
            tipsPanel.SetActive(false);
        }
    }

    // This method is called when the video ends or when stop is pressed
    public void tipping()
    {
        Debug.Log("Activating tipping interface");

        if (tippingActive)
            return;

        tippingActive = true;

        // Position and show the tips panel using our VR-friendly approach
        if (tipsController != null)
        {
            tipsController.ShowTipsPanel();
        }
        else if (tipsPanel != null)
        {
            // Legacy approach if controller not available
            PositionTipsPanelInFrontOfCamera();
            tipsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Cannot activate tipping: No tips panel found!");
        }
    }

    // Position the tips panel in front of the camera (used if IntegratedTipsController not available)
    private void PositionTipsPanelInFrontOfCamera()
    {
        if (tipsPanel != null && Camera.main != null)
        {
            // Position the panel 1.5 meters in front of the camera
            Vector3 panelPosition = Camera.main.transform.position + (Camera.main.transform.forward * 1.5f);
            tipsPanel.transform.position = panelPosition;

            // Ensure the panel faces the camera
            tipsPanel.transform.LookAt(2 * tipsPanel.transform.position - Camera.main.transform.position);
        }
    }

    // This can be called to dismiss the tipping panel
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

    // Call this method to start playing a video
    public void PlayVideo(string videoUrl)
    {
        if (!string.IsNullOrEmpty(videoUrl))
        {
            // Store video URL in PlayerPrefs for tipping system
            PlayerPrefs.SetString("VideoUrl", videoUrl);

            // If using scene-based approach
            PlayerPrefs.SetString("nextscene", "360VideoApp");
            SceneManager.LoadScene("360VideoApp");
        }
        else
        {
            Debug.LogError("Cannot play video: URL is empty!");
        }
    }
}