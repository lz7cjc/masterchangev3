using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional minimal loading indicator that can show the background loading progress.
/// </summary>
public class MinimalLoadingIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject indicatorPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text statusText;

    [Header("Display Options")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private bool hideWhenComplete = true;
    [SerializeField] private bool showPercentage = true;

    private BackgroundPreloader preloader;

    private void Start()
    {
        // Find the preloader
        preloader = BackgroundPreloader.Instance;

        if (preloader == null)
        {
            Debug.LogWarning("BackgroundPreloader not found. Loading indicator won't function.");
            return;
        }

        // Subscribe to events
        preloader.OnProgressChanged += UpdateProgress;
        preloader.OnPreloadComplete += OnPreloadingComplete;

        // Initial state
        if (showOnStart)
        {
            ShowIndicator();
        }
        else
        {
            HideIndicator();
        }

        // Initial progress
        UpdateProgress(preloader.PreloadProgress);
    }

    private void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (statusText != null && showPercentage)
        {
            statusText.text = $"{Mathf.Floor(progress * 100)}%";
        }
    }

    private void OnPreloadingComplete()
    {
        if (hideWhenComplete)
        {
            HideIndicator();
        }
    }

    public void ShowIndicator()
    {
        if (indicatorPanel != null)
        {
            indicatorPanel.SetActive(true);
        }
    }

    public void HideIndicator()
    {
        if (indicatorPanel != null)
        {
            indicatorPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (preloader != null)
        {
            preloader.OnProgressChanged -= UpdateProgress;
            preloader.OnPreloadComplete -= OnPreloadingComplete;
        }
    }
}