using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BackgroundPreloader : MonoBehaviour
{
    [SerializeField] private string[] assetsToPreload;
    [SerializeField] private string[] labelsToPreload;
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

    public event System.Action<float> OnProgressChanged;
    public event System.Action OnPreloadComplete;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LogMessage("Instance initialized and set to DontDestroyOnLoad");
        }
        else
        {
            LogMessage("Duplicate instance found, destroying");
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
        if ((assetsToPreload == null || assetsToPreload.Length == 0) &&
            (labelsToPreload == null || labelsToPreload.Length == 0))
        {
            LogMessage("No assets configured for preloading.");
            PreloadingComplete = true;
            OnPreloadComplete?.Invoke();
            yield break;
        }

        _isPreloading = true;
        int totalOperations = 0;

        if (assetsToPreload != null) totalOperations += assetsToPreload.Length;
        if (labelsToPreload != null) totalOperations += labelsToPreload.Length;

        int completedOperations = 0;

        LogMessage("Starting background preloading of addressable assets...");

        // Load assets by address
        if (assetsToPreload != null)
        {
            foreach (string address in assetsToPreload)
            {
                if (string.IsNullOrEmpty(address))
                {
                    LogMessage("Skipping empty address");
                    completedOperations++;
                    continue;
                }

                LogMessage($"Preloading asset: {address}");
                AsyncOperationHandle handle = default;

                try
                {
                    handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
                    _loadedAssets[address] = handle;
                }
                catch (System.Exception e)
                {
                    LogError($"Exception starting load of {address}: {e.Message}");
                    completedOperations++;
                    UpdateProgress((float)completedOperations / totalOperations);
                    continue;
                }

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
        }

        // Load assets by label
        if (labelsToPreload != null)
        {
            foreach (string label in labelsToPreload)
            {
                if (string.IsNullOrEmpty(label))
                {
                    LogMessage("Skipping empty label");
                    completedOperations++;
                    continue;
                }

                LogMessage($"Preloading assets with label: {label}");
                AsyncOperationHandle<IList<UnityEngine.Object>> handle = default;

                try
                {
                    handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(
                        label,
                        obj => { /* Optional callback for each loaded object */ }
                    );

                    // Store the handle with the label as key
                    _loadedAssets[label] = handle;
                }
                catch (System.Exception e)
                {
                    LogError($"Exception starting load of label {label}: {e.Message}");
                    completedOperations++;
                    UpdateProgress((float)completedOperations / totalOperations);
                    continue;
                }

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
        }

        _isPreloading = false;
        PreloadingComplete = true;
        LogMessage("Background preloading completed successfully.");

        // Notify completion
        OnPreloadComplete?.Invoke();
    }

    public List<T> GetLoadedAssetsByLabel<T>(string label) where T : UnityEngine.Object
    {
        List<T> result = new List<T>();

        if (string.IsNullOrEmpty(label))
        {
            LogError("Cannot get assets with empty label");
            return result;
        }

        if (_loadedAssets.TryGetValue(label, out AsyncOperationHandle handle))
        {
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (handle.Result is IList<UnityEngine.Object> objects)
                {
                    LogMessage($"Found {objects.Count} objects with label {label}");
                    foreach (var obj in objects)
                    {
                        if (obj is T matchingObj)
                        {
                            result.Add(matchingObj);
                        }
                    }
                    LogMessage($"Returning {result.Count} objects of type {typeof(T).Name}");
                }
                else
                {
                    LogError($"Result for label {label} is not a list");
                }
            }
            else
            {
                LogError($"Handle for label {label} is invalid or operation failed");
            }
        }
        else
        {
            LogError($"No assets loaded with label: {label}");
        }

        return result;
    }

    public T GetLoadedAsset<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            LogError("Cannot get asset with empty address");
            return null;
        }

        if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle handle))
        {
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (handle.Result is T result)
                {
                    return result;
                }
                else
                {
                    LogError($"Asset at {address} is not of type {typeof(T).Name}");
                }
            }
            else
            {
                LogError($"Handle for address {address} is invalid or operation failed");
            }
        }
        else
        {
            LogError($"No asset loaded with address: {address}");
        }

        return null;
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
        // Clear instance reference if we're the current instance
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // This method should be called when the application is shutting down
    public void ReleaseAllAssets()
    {
        foreach (var handle in _loadedAssets.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _loadedAssets.Clear();
        LogMessage("Released all loaded assets");
    }

    private void OnApplicationQuit()
    {
        ReleaseAllAssets();
    }
}