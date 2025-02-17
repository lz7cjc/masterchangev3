using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LoadingManager : MonoBehaviour
{
    [System.Serializable]
    public class AssetItem
    {
        public AssetReference asset;
        public string displayLabel;
        public float downloadProgress;
        public GameObject loadedObject;
    }

    [SerializeField] private GameObject downloadItem;
    [SerializeField] private AssetItem[] assetsToLoad;
    [SerializeField] private int maxConcurrentDownloads = 3;

    [Header("UI References")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI currentAssetText;
    [SerializeField] private TextMeshProUGUI downloadPercentText;

    private System.Threading.SemaphoreSlim downloadSemaphore;
    private int completedDownloads;
    private float totalProgress;
    private float totalDownloadProgress;
    private List<GameObject> loadedAssets = new List<GameObject>();

    void Start()
    {
        ValidateAssets();
        downloadSemaphore = new System.Threading.SemaphoreSlim(maxConcurrentDownloads);
        InitializeUI();
        LoadAddressables();
    }

    private void InitializeUI()
    {
        downloadItem.SetActive(true);
        progressBar.value = 0;
        progressText.text = "0%";
        currentAssetText.text = "Preparing...";
        downloadPercentText.text = "Percentage Complete: 0%";
        totalDownloadProgress = 0f;
    }

    private void ValidateAssets()
    {
        if (assetsToLoad == null || assetsToLoad.Length == 0)
        {
            Debug.LogError("No assets configured to load!");
            return;
        }

        foreach (var item in assetsToLoad)
        {
            item.downloadProgress = 0f;
            item.loadedObject = null;
        }
    }

    async void LoadAddressables()
    {
        var loadTasks = new List<Task>();
        completedDownloads = 0;
        totalProgress = 0;

        for (int i = 0; i < assetsToLoad.Length; i++)
        {
            if (assetsToLoad[i]?.asset == null) continue;
            loadTasks.Add(LoadAssetWithSemaphore(assetsToLoad[i], i));
        }

        await Task.WhenAll(loadTasks);

        foreach (var item in assetsToLoad)
        {
            if (item.loadedObject != null)
            {
                item.loadedObject.SetActive(true);
            }
        }

        downloadItem.SetActive(false);
    }

    private async Task LoadAssetWithSemaphore(AssetItem assetItem, int index)
    {
        try
        {
            await downloadSemaphore.WaitAsync();

            string displayName = string.IsNullOrEmpty(assetItem.displayLabel) ?
                $"Asset {index + 1}" : assetItem.displayLabel;

            currentAssetText.text = $"Loading {displayName}...";

            var sizeHandle = Addressables.GetDownloadSizeAsync(assetItem.asset);
            await sizeHandle.Task;

            var handle = Addressables.LoadAssetAsync<GameObject>(assetItem.asset);

            while (!handle.IsDone)
            {
                assetItem.downloadProgress = handle.PercentComplete;
                UpdateDownloadProgress();
                UpdateIndividualProgress(handle.PercentComplete);
                await Task.Yield();
            }

            var result = await handle.Task;
            if (result != null)
            {
                assetItem.loadedObject = Instantiate(result, transform);
                assetItem.loadedObject.SetActive(false);
                loadedAssets.Add(assetItem.loadedObject);
            }

            assetItem.downloadProgress = 1f;
            UpdateDownloadProgress();
            completedDownloads++;
            UpdateTotalProgress();
        }
        finally
        {
            downloadSemaphore.Release();
        }
    }

    private void UpdateDownloadProgress()
    {
        float totalProgress = 0f;
        foreach (var item in assetsToLoad)
        {
            totalProgress += item.downloadProgress;
        }

        totalDownloadProgress = (totalProgress / assetsToLoad.Length) * 100f;
        downloadPercentText.text = $"Percentage Complete: {totalDownloadProgress:F1}%";

        // Sync progress bar with download progress
        progressBar.value = totalDownloadProgress / 100f;
        progressText.text = $"{totalDownloadProgress:F0}%";
    }

    private void UpdateIndividualProgress(float percentComplete)
    {
        float currentWeight = 1f / assetsToLoad.Length;
        float adjustedProgress = (completedDownloads * currentWeight) + (percentComplete * currentWeight);
        // Progress bar update moved to UpdateDownloadProgress for consistency
    }

    private void UpdateTotalProgress()
    {
        totalProgress = (float)completedDownloads / assetsToLoad.Length;
        // Progress updates moved to UpdateDownloadProgress for consistency
    }

    private void OnDestroy()
    {
        downloadSemaphore?.Dispose();
        foreach (var asset in loadedAssets)
        {
            if (asset != null)
            {
                Destroy(asset);
            }
        }
    }
}