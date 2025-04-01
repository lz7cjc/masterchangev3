using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Preloads addressable assets in the background as soon as the app starts.
/// Place this on a GameObject in your initial scene.
/// </summary>
public class BackgroundPreloader : MonoBehaviour
{
    [Tooltip("Assets to preload by address")]
    [SerializeField] private string[] assetsToPreload;

    [Tooltip("Assets to preload by label")]
    [SerializeField] private string[] labelsToPreload;

    [Tooltip("Whether to show debug logs")]
    [SerializeField] private bool showDebugLogs = true;

    // Singleton instance
    private static BackgroundPreloader _instance;
    public static BackgroundPreloader Instance => _instance;

    // Tracking loaded assets
    private Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();
    private bool _isPreloading = false;
    private float _preloadProgress = 0f;

    // Properties and events
    public bool IsPreloading => _isPreloading;
    public float PreloadProgress => _preloadProgress;
    public bool PreloadingComplete { get; private set; } = false;

    public event Action<float> OnProgressChanged;
    public event Action OnPreloadComplete;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Start preloading immediately
        StartCoroutine(PreloadAssets());
    }

    private IEnumerator PreloadAssets()
    {
        if (assetsToPreload.Length == 0 && labelsToPreload.Length == 0)
        {
            LogMessage("No assets configured for preloading.");
            PreloadingComplete = true;
            OnPreloadComplete?.Invoke();
            yield break;
        }

        _isPreloading = true;
        int totalOperations = assetsToPreload.Length + labelsToPreload.Length;
        int completedOperations = 0;

        LogMessage("Starting background preloading of addressable assets...");

        // Load assets by address
        foreach (string address in assetsToPreload)
        {
            LogMessage($"Preloading asset: {address}");
            AsyncOperationHandle handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
            _loadedAssets[address] = handle;

            // Wait for completion (non-blocking)
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                LogError($"Failed to load asset: {address} - {handle.OperationException}");
            }
            else
            {
                LogMessage($"Successfully loaded asset: {address}");
            }

            completedOperations++;
            UpdateProgress((float)completedOperations / totalOperations);
        }

        // Load assets by label
        foreach (string label in labelsToPreload)
        {
            LogMessage($"Preloading assets with label: {label}");
            AsyncOperationHandle<IList<UnityEngine.Object>> handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(
                label,
                obj => { /* Optional callback for each loaded object */ }
            );

            // Store the handle with the label as key
            _loadedAssets[label] = handle;

            // Wait for completion (non-blocking)
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                LogError($"Failed to load assets with label: {label} - {handle.OperationException}");
            }
            else
            {
                LogMessage($"Successfully loaded {handle.Result.Count} assets with label: {label}");
            }

            completedOperations++;
            UpdateProgress((float)completedOperations / totalOperations);
        }

        _isPreloading = false;
        PreloadingComplete = true;
        LogMessage("Background preloading completed successfully.");

        // Notify completion
        OnPreloadComplete?.Invoke();
    }

    /// <summary>
    /// Gets an already loaded asset if it exists.
    /// </summary>
    /// <typeparam name="T">Type of asset to get</typeparam>
    /// <param name="key">Asset address or label</param>
    /// <returns>The loaded asset, or null if not found or wrong type</returns>
    public T GetLoadedAsset<T>(string key) where T : UnityEngine.Object
    {
        if (_loadedAssets.TryGetValue(key, out AsyncOperationHandle handle))
        {
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (handle.Result is T result)
                {
                    return result;
                }

                if (handle.Result is IList<UnityEngine.Object> objects)
                {
                    // This was a label load with multiple objects, find first matching type
                    foreach (var obj in objects)
                    {
                        if (obj is T matchingObj)
                        {
                            return matchingObj;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all loaded assets of a specific type from a label.
    /// </summary>
    /// <typeparam name="T">Type of assets to get</typeparam>
    /// <param name="label">Label used to load the assets</param>
    /// <returns>List of loaded assets of the specified type, or empty list if none found</returns>
    public List<T> GetLoadedAssetsByLabel<T>(string label) where T : UnityEngine.Object
    {
        List<T> result = new List<T>();

        if (_loadedAssets.TryGetValue(label, out AsyncOperationHandle handle))
        {
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (handle.Result is IList<UnityEngine.Object> objects)
                {
                    foreach (var obj in objects)
                    {
                        if (obj is T matchingObj)
                        {
                            result.Add(matchingObj);
                        }
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Manually preloads an additional asset if needed.
    /// </summary>
    public void PreloadAdditionalAsset(string address)
    {
        if (_loadedAssets.ContainsKey(address))
        {
            LogMessage($"Asset {address} already preloaded.");
            return;
        }

        LogMessage($"Preloading additional asset: {address}");
        AsyncOperationHandle handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
        _loadedAssets[address] = handle;
    }

    private void UpdateProgress(float progress)
    {
        _preloadProgress = progress;
        OnProgressChanged?.Invoke(progress);
        LogMessage($"Preload progress: {progress:P0}");
    }

    private void LogMessage(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[BackgroundPreloader] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BackgroundPreloader] {message}");
    }

    private void OnDestroy()
    {
        // Release all loaded assets when the preloader is destroyed
        foreach (var handle in _loadedAssets.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _loadedAssets.Clear();
    }
}