using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Networking;
using System;

public class DebugAddressableLoader : MonoBehaviour
{
    [Header("Addressable Labels")]
    [SerializeField] private string cottageLabelName = "Cottage";
    [SerializeField] private string terrainLabelName = "Terrain";

    [Header("Position References")]
    [SerializeField] private Transform cottagePosition;
    [SerializeField] private Transform terrainPosition;

    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button retryButton;

    // Log history
    private List<string> logHistory = new List<string>();
    private int maxLogLines = 15;

    private void Awake()
    {
        // Set up retry button
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryLoading);
            retryButton.gameObject.SetActive(false);
        }

        // Subscribe to log messages
        Application.logMessageReceived += OnLogMessage;
    }

    private void Start()
    {
        Debug.Log("DebugAddressableLoader started");
        StartCoroutine(RunNetworkTest());
        StartCoroutine(LoadAndPositionAddressables());
    }

    private IEnumerator RunNetworkTest()
    {
        AddLog("Running network connectivity test...");

        // Test Google connectivity
        using (UnityWebRequest request = UnityWebRequest.Get("https://storage.googleapis.com"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                AddLog($"Google Storage connectivity issue: {request.error}");
            }
            else
            {
                AddLog("Google Storage connection successful");
            }
        }

        // Also test general internet connectivity
        using (UnityWebRequest request = UnityWebRequest.Get("https://www.google.com"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                AddLog($"Internet connectivity issue: {request.error}");
            }
            else
            {
                AddLog("Internet connection successful");
            }
        }
    }

    private void AddLog(string message)
    {
        // Always log to console
        Debug.Log($"[DebugAddressableLoader] {message}");

        // Add to history and trim if needed
        logHistory.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
        while (logHistory.Count > maxLogLines)
        {
            logHistory.RemoveAt(0);
        }

        // Update UI if available
        if (debugText != null)
        {
            debugText.text = string.Join("\n", logHistory);
        }
    }

    private void OnLogMessage(string logString, string stackTrace, LogType type)
    {
        // Only capture addressable-related logs to avoid noise
        if (logString.Contains("Addressable") || logString.Contains("addressable") ||
            logString.Contains("Bundle") || logString.Contains("bundle") ||
            logString.Contains("Asset") || logString.Contains("asset"))
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                AddLog($"ERROR: {logString}");

                // Show retry button on errors
                if (retryButton != null)
                {
                    retryButton.gameObject.SetActive(true);
                }
            }
            else if (type == LogType.Warning)
            {
                AddLog($"WARNING: {logString}");
            }
        }
    }

    private void RetryLoading()
    {
        AddLog("Retrying asset loading...");
        StartCoroutine(LoadAndPositionAddressables());
    }

    private IEnumerator LoadAndPositionAddressables()
    {
        // Reset retry button
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
        }

        // Check if assets already exist in the scene
        GameObject existingCottage = GameObject.Find("Cottage_Addressable");
        GameObject existingTerrain = GameObject.Find("Terrain_Addressable");

        // If both assets already exist, no need to instantiate again
        if (existingCottage != null && existingTerrain != null)
        {
            AddLog("Addressable assets already exist in scene, skipping instantiation");
            yield break;
        }

        // First try to use the BackgroundPreloader
        BackgroundPreloader preloader = BackgroundPreloader.Instance;

        if (preloader != null)
        {
            AddLog("Found BackgroundPreloader instance");

            // If preloading is still in progress, show download progress
            if (!preloader.PreloadingComplete)
            {
                UpdateProgress("Waiting for background preloader to complete...", preloader.PreloadProgress);

                // Subscribe to progress updates
                preloader.OnProgressChanged += UpdateProgressFromEvent;

                // Wait for preloader to finish
                float timeout = 30f; // 30 second timeout
                float elapsed = 0f;

                while (!preloader.PreloadingComplete && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Unsubscribe when done
                preloader.OnProgressChanged -= UpdateProgressFromEvent;

                if (elapsed >= timeout)
                {
                    AddLog("Background preloader timed out - will try direct loading");
                    // Continue to direct loading
                }
                else
                {
                    AddLog("Background preloader completed successfully");
                }
            }
            else
            {
                AddLog("Background preloader already completed");
            }

            // Try to get assets from preloader
            List<GameObject> cottagePrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(cottageLabelName);
            List<GameObject> terrainPrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(terrainLabelName);

            AddLog($"Retrieved {cottagePrefabs.Count} cottage prefabs and {terrainPrefabs.Count} terrain prefabs from preloader");

            // Check if we got the assets from the preloader
            if (cottagePrefabs.Count > 0 && terrainPrefabs.Count > 0)
            {
                AddLog("Successfully retrieved assets from preloader");

                // Position cottage if it doesn't already exist
                if (existingCottage == null && cottagePosition != null)
                {
                    GameObject addressableCottage = Instantiate(cottagePrefabs[0]);
                    addressableCottage.transform.position = cottagePosition.position;
                    addressableCottage.transform.rotation = cottagePosition.rotation;
                    addressableCottage.transform.localScale = cottagePosition.localScale;
                    addressableCottage.name = "Cottage_Addressable";
                    AddLog("Instantiated cottage from preloader");
                }

                // Position terrain if it doesn't already exist
                if (existingTerrain == null && terrainPosition != null)
                {
                    GameObject addressableTerrain = Instantiate(terrainPrefabs[0]);
                    addressableTerrain.transform.position = terrainPosition.position;
                    addressableTerrain.transform.rotation = terrainPosition.rotation;
                    addressableTerrain.transform.localScale = terrainPosition.localScale;
                    addressableTerrain.name = "Terrain_Addressable";
                    AddLog("Instantiated terrain from preloader");
                }

                UpdateProgress("Assets loaded successfully", 1f);
                yield break; // Done with preloader approach
            }
            else
            {
                AddLog("Could not get assets from preloader, trying direct loading");
            }
        }
        else
        {
            AddLog("No BackgroundPreloader found, using direct loading instead");
        }

        // If we reach here, we need to use direct loading
        AddLog("Attempting direct loading of addressable assets");

        // Load cottage
        if (existingCottage == null)
        {
            AddLog($"Loading cottage with label: {cottageLabelName}");
            AsyncOperationHandle<IList<GameObject>> cottageOperation = default;

            try
            {
                UpdateProgress("Loading cottage...", 0.1f);
                cottageOperation = Addressables.LoadAssetsAsync<GameObject>(
                    cottageLabelName,
                    obj => {
                        AddLog($"Asset loaded callback: {obj.name}");
                    }
                );
            }
            catch (Exception e)
            {
                AddLog($"Exception starting cottage load: {e.Message}");
                Debug.LogException(e);
            }

            // Only proceed if the operation was successfully started
            if (cottageOperation.IsValid())
            {
                // Track progress outside the try-catch
                while (!cottageOperation.IsDone)
                {
                    UpdateProgress($"Loading cottage... {cottageOperation.PercentComplete:P0}", cottageOperation.PercentComplete * 0.5f);
                    yield return null;
                }

                try
                {
                    if (cottageOperation.Status == AsyncOperationStatus.Succeeded)
                    {
                        AddLog($"Cottage loaded successfully with {cottageOperation.Result.Count} objects");

                        if (cottageOperation.Result.Count > 0 && cottagePosition != null)
                        {
                            GameObject addressableCottage = Instantiate(cottageOperation.Result[0]);
                            addressableCottage.transform.position = cottagePosition.position;
                            addressableCottage.transform.rotation = cottagePosition.rotation;
                            addressableCottage.transform.localScale = cottagePosition.localScale;
                            addressableCottage.name = "Cottage_Addressable";
                            AddLog("Cottage instantiated successfully");
                        }
                        else
                        {
                            AddLog("Cottage loaded but no objects found or position missing");
                        }
                    }
                    else
                    {
                        AddLog($"Failed to load cottage: {cottageOperation.OperationException}");
                    }
                }
                catch (Exception e)
                {
                    AddLog($"Exception processing cottage results: {e.Message}");
                    Debug.LogException(e);
                }
            }
            else
            {
                AddLog("Failed to start cottage loading operation");
            }
        }

        // Load terrain
        if (existingTerrain == null)
        {
            AddLog($"Loading terrain with label: {terrainLabelName}");
            AsyncOperationHandle<IList<GameObject>> terrainOperation = default;

            try
            {
                UpdateProgress("Loading terrain...", 0.5f);
                terrainOperation = Addressables.LoadAssetsAsync<GameObject>(
                    terrainLabelName,
                    obj => {
                        AddLog($"Asset loaded callback: {obj.name}");
                    }
                );
            }
            catch (Exception e)
            {
                AddLog($"Exception starting terrain load: {e.Message}");
                Debug.LogException(e);
            }

            // Only proceed if the operation was successfully started
            if (terrainOperation.IsValid())
            {
                // Track progress outside the try-catch
                while (!terrainOperation.IsDone)
                {
                    UpdateProgress($"Loading terrain... {terrainOperation.PercentComplete:P0}", 0.5f + terrainOperation.PercentComplete * 0.5f);
                    yield return null;
                }

                try
                {
                    if (terrainOperation.Status == AsyncOperationStatus.Succeeded)
                    {
                        AddLog($"Terrain loaded successfully with {terrainOperation.Result.Count} objects");

                        if (terrainOperation.Result.Count > 0 && terrainPosition != null)
                        {
                            GameObject addressableTerrain = Instantiate(terrainOperation.Result[0]);
                            addressableTerrain.transform.position = terrainPosition.position;
                            addressableTerrain.transform.rotation = terrainPosition.rotation;
                            addressableTerrain.transform.localScale = terrainPosition.localScale;
                            addressableTerrain.name = "Terrain_Addressable";
                            AddLog("Terrain instantiated successfully");
                        }
                        else
                        {
                            AddLog("Terrain loaded but no objects found or position missing");
                        }
                    }
                    else
                    {
                        AddLog($"Failed to load terrain: {terrainOperation.OperationException}");
                    }
                }
                catch (Exception e)
                {
                    AddLog($"Exception processing terrain results: {e.Message}");
                    Debug.LogException(e);
                }
            }
            else
            {
                AddLog("Failed to start terrain loading operation");
            }
        }

        UpdateProgress("Asset loading complete", 1f);
    }

    private void UpdateProgressFromEvent(float progress)
    {
        UpdateProgress("Loading assets...", progress);
    }

    private void UpdateProgress(string status, float progress)
    {
        // Always log progress to console for debugging
        Debug.Log($"[DebugAddressableLoader] Progress: {status} ({progress:P0})");

        // Update progress UI
        if (progressText != null)
        {
            progressText.text = $"{status} ({Mathf.Floor(progress * 100)}%)";
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        Application.logMessageReceived -= OnLogMessage;

        // Check if preloader exists and unsubscribe
        BackgroundPreloader preloader = BackgroundPreloader.Instance;
        if (preloader != null)
        {
            preloader.OnProgressChanged -= UpdateProgressFromEvent;
        }
    }
}