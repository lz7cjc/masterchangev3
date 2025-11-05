using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VRLoadingManager : MonoBehaviour
{
    public static VRLoadingManager Instance;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI statusText;      // CHANGE
    [SerializeField] private TextMeshProUGUI percentageText;  // CHANGE
    [SerializeField] private Image progressBar;

    private float currentProgress = 0f;
    private float targetProgress = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void Update()
    {
        if (Mathf.Abs(currentProgress - targetProgress) > 0.01f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 2f);
            UpdateUI();
        }
    }

    public void ShowLoading(string message, float initialProgress = 0f)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        currentProgress = initialProgress;
        targetProgress = initialProgress;

        if (statusText != null)
            statusText.text = message;

        UpdateUI();
    }

    public void HideLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        currentProgress = 0f;
        targetProgress = 0f;
    }

    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void UpdateUI()
    {
        if (percentageText != null)
            percentageText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";

        if (progressBar != null)
            progressBar.fillAmount = currentProgress;
    }

    public void ShowSwitchToVR()
    {
        ShowLoading("Switching to VR mode...", 0f);
    }

    public void ShowSwitchTo360()
    {
        ShowLoading("Switching to 360 mode...", 0f);
    }
}