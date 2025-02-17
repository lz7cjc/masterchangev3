using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DownloadManager : MonoBehaviour
{
    public static DownloadManager Instance { get; private set; }
    [SerializeField] private Canvas downloadCanvas;
    [SerializeField] private GameObject downloadStatusPrefab;
    private DownloadStatus activeStatus;
    private float totalSize;
    private float downloadedSize;
    private int activeDownloads;

    private void Awake()
    {
        Debug.Log("DownloadManager Awake");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (downloadCanvas == null)
        {
            downloadCanvas = GetComponent<Canvas>();
            Debug.Log($"Canvas found: {downloadCanvas != null}");
        }

        downloadCanvas.enabled = false;
    }

    public async void LoadAddressableGroup(string groupName)
    {
        Debug.Log($"Loading group: {groupName}");

        if (activeStatus == null)
        {
            Debug.Log("Creating download status");
            CreateDownloadStatus("TotalProgress", "Downloading Assets");
        }

        activeDownloads++;
        float groupSize = await Addressables.GetDownloadSizeAsync(groupName).Task;
        Debug.Log($"Group size: {groupSize}");
        totalSize += groupSize;

        if (groupSize == 0)
        {
            Debug.Log("No downloads needed - content may be cached");
            activeDownloads--;
            if (activeDownloads == 0) RemoveDownloadStatus();
            return;
        }

        var operation = Addressables.DownloadDependenciesAsync(groupName);

        while (!operation.IsDone)
        {
            float currentDownloaded = operation.GetDownloadStatus().DownloadedBytes;
            downloadedSize = (downloadedSize + currentDownloaded) - (groupSize * operation.PercentComplete);
            float totalProgress = Mathf.Clamp01(downloadedSize / totalSize);

            Debug.Log($"Progress: {totalProgress:P0}");
            UpdateProgress(totalProgress);
            await System.Threading.Tasks.Task.Yield();
        }

        activeDownloads--;
        if (activeDownloads == 0)
        {
            Debug.Log("All downloads complete");
            await System.Threading.Tasks.Task.Delay(500);
            RemoveDownloadStatus();
        }

        Addressables.Release(operation);
    }

    private void CreateDownloadStatus(string guid, string title)
    {
        downloadCanvas.enabled = true;
        GameObject statusObj = Instantiate(downloadStatusPrefab, downloadCanvas.transform);
        activeStatus = statusObj.GetComponent<DownloadStatus>();

        if (activeStatus != null)
        {
            activeStatus.Initialize(guid, title);
            Debug.Log("Download status created and initialized");
        }
        else
        {
            Debug.LogError("Failed to get DownloadStatus component");
        }
    }

    private void UpdateProgress(float progress)
    {
        if (activeStatus == null)
        {
            Debug.LogWarning("No active status to update");
            return;
        }
        activeStatus.UpdateProgress(progress, 0f);
    }

    private void RemoveDownloadStatus()
    {
        if (activeStatus == null) return;

        Destroy(activeStatus.gameObject);
        activeStatus = null;
        downloadCanvas.enabled = false;
        Debug.Log("Download status removed");
    }
}