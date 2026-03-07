using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class AddressableLoader : MonoBehaviour
{
    [Header("Position References")]
    public Transform cottagePosition;
    public Transform terrainPosition;
    public Transform mountainPosition;
    public Transform islandPosition;
    public Transform travelCheckinPosition;

    [Header("Loading UI (Optional)")]
    public GameObject loadingPanel;

    [Header("Asset Labels")]
    public string[] labelsToLoad = new string[] { "Cottage", "Terrain", "Mountain", "Island", "Travel" };

    private List<AsyncOperationHandle> loadedHandles = new List<AsyncOperationHandle>();

    void Start()
    {
        // Show loading UI
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        Debug.Log("Starting to load addressables...");

        // Load all addressables
        LoadAllAddressables();
    }

    void LoadAllAddressables()
    {
        foreach (string label in labelsToLoad)
        {
            LoadAssetByLabel(label);
        }
    }

    void LoadAssetByLabel(string label)
    {
        Debug.Log("Loading addressable with label: " + label);

        // Load asset by label
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(label);
        loadedHandles.Add(handle);

        // Set up callback when loading completes
        handle.Completed += (op) => OnAssetLoaded(op, label);
    }

    void OnAssetLoaded(AsyncOperationHandle<GameObject> handle, string label)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;
            Transform spawnPosition = GetPositionForLabel(label);

            if (spawnPosition != null)
            {
                GameObject instance = Instantiate(prefab, spawnPosition.position, spawnPosition.rotation);
                Debug.Log("✅ " + label + " instantiated successfully at " + spawnPosition.name);
            }
            else
            {
                Debug.LogWarning("⚠️ No position marker found for " + label);
            }
        }
        else
        {
            Debug.LogError("❌ Failed to load " + label + ": " + handle.OperationException);
        }

        // Check if all done
        CheckIfAllLoaded();
    }

    void CheckIfAllLoaded()
    {
        bool allDone = true;
        foreach (var handle in loadedHandles)
        {
            if (!handle.IsDone)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            Debug.Log("All addressables loaded!");

            // Hide loading UI
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }
    }

    Transform GetPositionForLabel(string label)
    {
        string lowerLabel = label.ToLower();

        if (lowerLabel == "cottage")
            return cottagePosition;
        else if (lowerLabel == "terrain")
            return terrainPosition;
        else if (lowerLabel == "mountain")
            return mountainPosition;
        else if (lowerLabel == "island")
            return islandPosition;
        else if (lowerLabel == "travel")
            return travelCheckinPosition;
        else
            return null;
    }

    void OnDestroy()
    {
        // Release all loaded addressables
        foreach (var handle in loadedHandles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        loadedHandles.Clear();
    }
}