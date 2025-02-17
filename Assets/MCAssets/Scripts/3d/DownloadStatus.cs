// DownloadStatus.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DownloadStatus : MonoBehaviour
{
    public string assetGuid;
    public TextMeshProUGUI titleText;
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public Button cancelButton;

    private void OnEnable()
    {
        if (progressBar != null) progressBar.value = 0;
        if (statusText != null) statusText.text = "Starting...";
    }

    public void Initialize(string guid, string title)
    {
        assetGuid = guid;
        if (titleText != null) titleText.text = title;
        if (progressBar != null) progressBar.value = 0;
        if (statusText != null) statusText.text = "Starting...";
    }

    public void UpdateProgress(float progress, float unused)
    {
        if (progressBar != null) progressBar.value = progress;
        if (statusText != null)
        {
            statusText.text = $"Download Progress: {(progress * 100):F0}%";
        }
    }
}