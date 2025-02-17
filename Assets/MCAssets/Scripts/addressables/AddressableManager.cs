using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class AddressableManager : MonoBehaviour
{
    public delegate void LoadProgressCallback(float progress, string assetName);
    public delegate void LoadCompleteCallback(bool success, string assetName);

    [System.Serializable]
    public class AddressableItem
    {
        public AssetReference assetReference;
        public bool loadOnStart = false;
        public bool keepLoaded = false;

        [NonSerialized]
        public AsyncOperationHandle<GameObject> loadHandle;
        [NonSerialized]
        public GameObject spawnedObject;
    }

    [System.Serializable]
    public class AddressableGroup
    {
        public string groupName;
        public List<AssetReference> assets = new List<AssetReference>();
        public bool loadOnStart = false;
        public bool keepLoaded = false;

        [NonSerialized]
        public List<AsyncOperationHandle<GameObject>> loadHandles = new List<AsyncOperationHandle<GameObject>>();
        [NonSerialized]
        public List<GameObject> spawnedObjects = new List<GameObject>();
    }

    [SerializeField] private GameObject downloadItemPrefab;
    [SerializeField] private Transform downloadContainer;
    [SerializeField] private int maxConcurrentDownloads = 3;
    [SerializeField] private float timeoutSeconds = 30f;
    [SerializeField] private bool enableDetailedLogs = true;

    [SerializeField] private List<AddressableItem> addressableItems = new List<AddressableItem>();
    [SerializeField] private List<AddressableGroup> addressableGroups = new List<AddressableGroup>();

    private readonly Dictionary<string, GameObject> downloadIndicators = new Dictionary<string, GameObject>();
    private readonly HashSet<string> activeDownloads = new HashSet<string>();
    private readonly Dictionary<string, float> downloadProgress = new Dictionary<string, float>();
    private readonly System.Threading.SemaphoreSlim downloadSemaphore;
    private LoadProgressCallback progressCallback;
    private LoadCompleteCallback completeCallback;
    private bool isInitialized;
    private bool isPaused;
    private Transform cachedTransform;
    private Camera mainCamera;
    private bool hasInternetPermission = false;

    public AddressableManager()
    {
        downloadSemaphore = new System.Threading.SemaphoreSlim(maxConcurrentDownloads);
    }


    private bool IsInternetAvailable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void LogManifestAndPermissions()
    {
        ////Debug.Log("=== Android Manifest & Permissions Debug ===");

#if UNITY_ANDROID
        // Check if we can access the internet
        hasInternetPermission = Application.internetReachability != NetworkReachability.NotReachable;
        ////Debug.Log($"Internet access available: {hasInternetPermission}");
        ////Debug.Log($"Network reachability: {Application.internetReachability}");

        // Check manifest file
        string manifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
        if (File.Exists(manifestPath))
        {
            ////Debug.Log($"Manifest found at: {manifestPath}");
        }
        else
        {
            ////Debug.LogWarning("AndroidManifest.xml not found in Plugins/Android folder");
            ////Debug.Log("Please ensure the following permissions are in your manifest:");
            ////Debug.Log("<uses-permission android:name=\"android.permission.INTERNET\" />");
            ////Debug.Log("<uses-permission android:name=\"android.permission.ACCESS_NETWORK_STATE\" />");
        }

        ////Debug.Log($"Platform: {Application.platform}");
        ////Debug.Log($"Unity version: {Application.unityVersion}");
        ////Debug.Log($"Company name: {Application.companyName}");
        ////Debug.Log($"Product name: {Application.productName}");
        ////Debug.Log($"Version: {Application.version}");
        ////Debug.Log($"Persistent data path: {Application.persistentDataPath}");
#else
            ////Debug.Log("Not running on Android platform");
#endif

        ////Debug.Log("===============================");
    }

    private void Awake()
    {
        cachedTransform = transform;
        mainCamera = Camera.main;
        Application.lowMemory += OnLowMemory;
        LogSystemInfo();
    }

    private void OnEnable()
    {
        isPaused = false;
    }

    private void OnDisable()
    {
        isPaused = true;
    }

    private void Start()
    {
        ////Debug.Log($"AddressableManager: Starting initial load at position {transform.position}");

        LogManifestAndPermissions();

        if (mainCamera != null)
        {
            ////Debug.Log($"Camera position: {mainCamera.transform.position}, rotation: {mainCamera.transform.rotation.eulerAngles}");
        }
        else
        {
            ////Debug.LogError("Main camera not found!");
        }

        DebugPrintGroupInfo("RemoteAssets");
        _ = InitializeAsync();
    }

    private void Update()
    {
        if (enableDetailedLogs && Time.frameCount % 300 == 0)
        {
            LogPositionsDebug();
        }
    }

    private void LogSystemInfo()
    {
        ////Debug.Log($"=== System Info ===");
        ////Debug.Log($"Platform: {Application.platform}");
        ////Debug.Log($"System memory size: {SystemInfo.systemMemorySize}MB");
        ////Debug.Log($"Device model: {SystemInfo.deviceModel}");
        ////Debug.Log($"Device type: {SystemInfo.deviceType}");
        ////Debug.Log($"Operating system: {SystemInfo.operatingSystem}");
        ////Debug.Log($"Processor type: {SystemInfo.processorType}");
        ////Debug.Log($"Processor count: {SystemInfo.processorCount}");
        ////Debug.Log($"Graphics device name: {SystemInfo.graphicsDeviceName}");
        ////Debug.Log($"Graphics memory size: {SystemInfo.graphicsMemorySize}MB");
        ////Debug.Log($"=================");
    }

    private void LogPositionsDebug()
    {
        ////Debug.Log("=== Position Debug Info ===");
        if (mainCamera != null)
        {
            ////Debug.Log($"Camera position: {mainCamera.transform.position}, rotation: {mainCamera.transform.rotation.eulerAngles}");
        }

        ////Debug.Log($"AddressableManager position: {transform.position}");

        foreach (var item in addressableItems)
        {
            if (item.spawnedObject != null)
            {
                ////Debug.Log($"Item {item.assetReference.RuntimeKey}: Position = {item.spawnedObject.transform.position}, " + $"Active = {item.spawnedObject.activeSelf}, " +  $"Parent = {item.spawnedObject.transform.parent?.name ?? "null"}");
            }
        }

        foreach (var group in addressableGroups)
        {
            ////Debug.Log($"Group: {group.groupName}");
            for (int i = 0; i < group.spawnedObjects.Count; i++)
            {
                var obj = group.spawnedObjects[i];
                if (obj != null)
                {
                    ////Debug.Log($"Group object {i}: Position = {obj.transform.position}, " + $"Active = {obj.activeSelf}, " + $"Parent = {obj.transform.parent?.name ?? "null"}");
                }
                {
                    ////Debug.Log($"Group object {i}: NULL");
                }
            }
        }
        ////Debug.Log("=========================");
    }

    public void DebugPrintGroupInfo(string groupName)
    {
        var group = FindGroup(groupName);
        if (group == null)
        {
            ////Debug.LogError($"Group '{groupName}' not found");
            return;
        }

        ////Debug.Log($"=== Group Debug Info: {groupName} ===");
        ////Debug.Log($"Total assets: {group.assets?.Count ?? 0}");
        ////Debug.Log($"LoadOnStart: {group.loadOnStart}");
        ////Debug.Log($"KeepLoaded: {group.keepLoaded}");

        if (group.assets != null)
        {
            for (int i = 0; i < group.assets.Count; i++)
            {
                var asset = group.assets[i];
                if (asset == null)
                {
                    ////Debug.Log($"Asset {i}: NULL");
                    continue;
                }

                ////Debug.Log($"Asset {i}:");
                ////Debug.Log($"  - RuntimeKey: {asset.RuntimeKey?.ToString() ?? "NULL"}");
                ////Debug.Log($"  - SubObjectName: {asset.SubObjectName}");
                ////Debug.Log($"  - AssetGUID: {asset.AssetGUID}");
                ////Debug.Log($"  - Internal ID: {asset.RuntimeKey.ToString()}");
            }
        }
        ////Debug.Log("=== End Group Debug Info ===");
    }

    public void SetCallbacks(LoadProgressCallback progress, LoadCompleteCallback complete)
    {
        progressCallback = progress;
        completeCallback = complete;
    }

    private async Task InitializeAsync()
    {
        if (isInitialized)
        {
            ////Debug.Log("Already initialized");
            return;
        }

        ////Debug.Log("Starting initialization");
        try
        {
            await Addressables.InitializeAsync().Task;
            ////Debug.Log("Addressables initialization complete");

            var loadTasks = new List<Task>();

            foreach (var item in addressableItems.Where(item => item.loadOnStart))
            {
                ////Debug.Log($"Queueing item for load: {item.assetReference.RuntimeKey}");
                loadTasks.Add(LoadAndSpawnItemWithTimeout(item));
            }

            foreach (var group in addressableGroups.Where(group => group.loadOnStart))
            {
                ////Debug.Log($"Queueing group for load: {group.groupName}");
                loadTasks.Add(LoadAndSpawnGroupWithTimeout(group));
            }

            await Task.WhenAll(loadTasks);
            isInitialized = true;
            ////Debug.Log("All initial loads complete");
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Initialization failed: {e.Message}\nStack trace: {e.StackTrace}");
            await CleanupFailedInitialization();
        }
    }

    private async Task CleanupFailedInitialization()
    {
        ////Debug.Log("Starting cleanup of failed initialization");
        foreach (var item in addressableItems)
        {
            await UnloadItemAsync(item);
        }

        foreach (var group in addressableGroups)
        {
            await UnloadGroupAsync(group);
        }
        ////Debug.Log("Cleanup complete");
    }

    private bool ValidateItem(AddressableItem item, string context)
    {
        if (item == null)
        {
            ////Debug.LogError($"{context}: Item is null");
            return false;
        }

        if (item.assetReference == null)
        {
            ////Debug.LogError($"{context}: Asset reference is null");
            return false;
        }

        if (item.assetReference.RuntimeKey == null)
        {
            ////Debug.LogError($"{context}: Runtime key is null for asset reference");
            return false;
        }

        return true;
    }

    private bool ValidateGroup(AddressableGroup group, string context)
    {
        if (group == null)
        {
            ////Debug.LogError($"{context}: Group is null");
            return false;
        }

        ////Debug.Log($"Validating group: {group.groupName}");

        if (group.assets == null)
        {
            ////Debug.LogError($"{context}: Assets list is null for group {group.groupName}");
            return false;
        }

        var validAssets = group.assets.Where(a => a != null && a.RuntimeKey != null).ToList();

        if (validAssets.Count == 0)
        {
            ////Debug.LogError($"{context}: No valid assets found in group {group.groupName}");
            return false;
        }

        ////Debug.Log($"Group {group.groupName} has {validAssets.Count} valid assets");
        return true;
    }

    private async Task LoadAndSpawnItemWithTimeout(AddressableItem item)
    {
        if (!ValidateItem(item, "LoadAndSpawnItemWithTimeout"))
            return;

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await downloadSemaphore.WaitAsync();
            await Task.WhenAny(
                LoadAndSpawnItem(item),
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token)
            );
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Timeout or error loading item: {e.Message}");
            await UnloadItemAsync(item);
        }
        finally
        {
            downloadSemaphore.Release();
        }
    }

    private async Task LoadAndSpawnGroupWithTimeout(AddressableGroup group)
    {
        if (!ValidateGroup(group, "LoadAndSpawnGroupWithTimeout"))
            return;

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await downloadSemaphore.WaitAsync();
            await Task.WhenAny(
                LoadAndSpawnGroup(group),
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token)
            );
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Timeout or error loading group {group.groupName}: {e.Message}");
            await UnloadGroupAsync(group);
        }
        finally
        {
            downloadSemaphore.Release();
        }
    }

    private async Task<GameObject> LoadAndSpawnItem(AddressableItem item)
    {
        if (isPaused || !ValidateItem(item, "LoadAndSpawnItem"))
            return null;

        string assetKey = item.assetReference.RuntimeKey.ToString();
        ////Debug.Log($"Starting to load item: {assetKey}");

        if (!IsInternetAvailable())
        {
            ////Debug.LogWarning($"No internet connection available when trying to load {assetKey}. HasPermission: {hasInternetPermission}");
            try
            {
                item.loadHandle = Addressables.LoadAssetAsync<GameObject>(item.assetReference);
                var result = await item.loadHandle.Task;
                if (result != null)
                {
                    ////Debug.Log($"Successfully loaded {assetKey} from cache");
                    item.spawnedObject = Instantiate(result, cachedTransform);
                    return item.spawnedObject;
                }
            }
            catch (Exception e)
            {
                ////Debug.LogError($"Failed to load {assetKey} from cache: {e.Message}");
                return null;
            }
        }

        try
        {
            if (!await ShowDownloadIndicatorAsync(item.assetReference))
                return null;

            var sizeHandle = Addressables.GetDownloadSizeAsync(item.assetReference);
            long size = await sizeHandle.Task;
            ////Debug.Log($"Download size for {assetKey}: {size} bytes");
            Addressables.Release(sizeHandle);

            item.loadHandle = Addressables.LoadAssetAsync<GameObject>(item.assetReference);
            ////Debug.Log($"Created load handle for {assetKey}");

            item.loadHandle.Completed += (operation) =>
            {
                ////Debug.Log($"Load completed for {assetKey}. Status: {operation.Status}");
                completeCallback?.Invoke(operation.Status == AsyncOperationStatus.Succeeded, assetKey);
            };

            var result = await item.loadHandle.Task;

            if (item.loadHandle.Status == AsyncOperationStatus.Succeeded && result != null)
            {
                if (!isPaused)
                {
                    ////Debug.Log($"Spawning {assetKey} at position {cachedTransform.position}");
                    item.spawnedObject = Instantiate(result, cachedTransform);
                    ////Debug.Log($"Spawned {assetKey} at position {item.spawnedObject.transform.position}");
                    return item.spawnedObject;
                }
            }
            else
            {
                ////Debug.LogError($"Failed to load addressable asset: {assetKey}. Status: {item.loadHandle.Status}");
                if (item.loadHandle.OperationException != null)
                {
                    ////Debug.LogError($"Exception: {item.loadHandle.OperationException}");
                }
            }

            return null;
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Error loading addressable asset {assetKey}: {e.Message}\nStack trace: {e.StackTrace}");
            return null;
        }
        finally
        {
            HideDownloadIndicator(item.assetReference);
            activeDownloads.Remove(assetKey);
            downloadProgress.Remove(assetKey);
        }
    }

    private async Task LoadAndSpawnGroup(AddressableGroup group)
    {
        if (!ValidateGroup(group, "LoadAndSpawnGroup"))
            return;

        var validAssets = group.assets.Where(a => a != null).ToList();
        ////Debug.Log($"Loading group {group.groupName} with {validAssets.Count} assets");

        try
        {
            foreach (var asset in validAssets)
            {
                await LoadGroupAsset(group, asset);
            }

            ////Debug.Log($"Successfully loaded group {group.groupName}");
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Error loading group {group.groupName}: {e.Message}");
            await UnloadGroupAsync(group);
        }
    }

    private async Task LoadGroupAsset(AddressableGroup group, AssetReference asset)
    {
        if (isPaused) return;

        string assetKey = asset.RuntimeKey.ToString();
        ////Debug.Log($"Loading asset {assetKey} in group {group.groupName}");

        try
        {
            if (!await ShowDownloadIndicatorAsync(asset))
                return;

            var sizeHandle = Addressables.GetDownloadSizeAsync(asset);
            long size = await sizeHandle.Task;
            ////Debug.Log($"Download size for {assetKey}: {size} bytes");
            Addressables.Release(sizeHandle);

            var loadHandle = Addressables.LoadAssetAsync<GameObject>(asset);
            loadHandle.Completed += (operation) =>
            {
                ////Debug.Log($"Load completed for {assetKey} in group {group.groupName}. Status: {operation.Status}");
                completeCallback?.Invoke(operation.Status == AsyncOperationStatus.Succeeded, assetKey);
            };

            await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded && !isPaused)
            {
                var spawnedObject = Instantiate(loadHandle.Result, cachedTransform);
                group.loadHandles.Add(loadHandle);
                group.spawnedObjects.Add(spawnedObject);
                ////Debug.Log($"Spawned {assetKey} at position {spawnedObject.transform.position}");
            }
            else
            {
                ////Debug.LogError($"Failed to load asset {assetKey} in group {group.groupName}");
                Addressables.Release(loadHandle);
            }
        }
        catch (Exception e)
        {
            ////Debug.LogError($"Error loading group asset {assetKey}: {e.Message}");
        }
        finally
        {
            HideDownloadIndicator(asset);
            activeDownloads.Remove(assetKey);
            downloadProgress.Remove(assetKey);
        }
    }

    private async Task<bool> ShowDownloadIndicatorAsync(AssetReference asset)
    {
        if (isPaused) return false;

        string key = asset.RuntimeKey.ToString();
        ////Debug.Log($"Showing download indicator for {key}");

        if (activeDownloads.Contains(key))
        {
            ////Debug.Log($"Download already in progress for {key}");
            return false;
        }

        activeDownloads.Add(key);
        downloadProgress[key] = 0f;

        if (downloadItemPrefab != null && downloadContainer != null)
        {
            await Task.Yield();
            var indicator = Instantiate(downloadItemPrefab, downloadContainer);
            downloadIndicators[key] = indicator;
            ////Debug.Log($"Created download indicator for {key}");
        }
        else
        {
            ////Debug.Log("Download indicator prefab or container is null");
        }

        return true;
    }

    private void HideDownloadIndicator(AssetReference asset)
    {
        string key = asset.RuntimeKey.ToString();
        //Debug.Log($"Hiding download indicator for {key}");

        if (downloadIndicators.TryGetValue(key, out GameObject indicator))
        {
            if (indicator != null)
            {
                Destroy(indicator);
                //Debug.Log($"Destroyed download indicator for {key}");
            }
            downloadIndicators.Remove(key);
        }
    }

    private void OnLowMemory()
    {
        //Debug.LogWarning("OnLowMemory called - cleaning up resources");
        UnloadNonEssentialAssets();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private async void UnloadNonEssentialAssets()
    {
        //Debug.Log("Unloading non-essential assets");
        foreach (var item in addressableItems.Where(i => !i.keepLoaded))
        {
            await UnloadItemAsync(item);
        }

        foreach (var group in addressableGroups.Where(g => !g.keepLoaded))
        {
            await UnloadGroupAsync(group);
        }
        //Debug.Log("Finished unloading non-essential assets");
    }

    private async Task UnloadItemAsync(AddressableItem item)
    {
        if (item == null) return;

        try
        {
            if (item.spawnedObject != null)
            {
                //Debug.Log($"Destroying spawned object for item {item.assetReference.RuntimeKey}");
                Destroy(item.spawnedObject);
                item.spawnedObject = null;
            }

            if (item.loadHandle.IsValid())
            {
                //Debug.Log($"Releasing handle for item {item.assetReference.RuntimeKey}");
                Addressables.Release(item.loadHandle);
                await Task.Yield();
            }
        }
        catch (Exception e)
        {
            //Debug.LogError($"Error unloading item: {e.Message}");
        }
    }

    private async Task UnloadGroupAsync(AddressableGroup group)
    {
        if (group == null) return;

        try
        {
            //Debug.Log($"Unloading group: {group.groupName}");
            foreach (var obj in group.spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            group.spawnedObjects.Clear();

            foreach (var handle in group.loadHandles.Where(h => h.IsValid()))
            {
                Addressables.Release(handle);
            }
            await Task.Yield();
            group.loadHandles.Clear();
        }
        catch (Exception e)
        {
            //Debug.LogError($"Error unloading group: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        Application.lowMemory -= OnLowMemory;
        downloadSemaphore?.Dispose();
        //Debug.Log("AddressableManager destroyed");
        _ = CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        foreach (var item in addressableItems)
        {
            await UnloadItemAsync(item);
        }

        foreach (var group in addressableGroups)
        {
            await UnloadGroupAsync(group);
        }

        foreach (var indicator in downloadIndicators.Values)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        downloadIndicators.Clear();
        downloadProgress.Clear();
    }

    public void UpdateProgress(string assetKey, float progress)
    {
        if (downloadProgress.ContainsKey(assetKey))
        {
            downloadProgress[assetKey] = progress;
            progressCallback?.Invoke(progress, assetKey);
        }
    }

    public AddressableItem FindItem(string assetGUID) =>
        addressableItems.FirstOrDefault(item =>
            item.assetReference != null &&
            item.assetReference.RuntimeKey.ToString() == assetGUID);

    public AddressableGroup FindGroup(string groupName) =>
        addressableGroups.FirstOrDefault(group => group.groupName == groupName);
}

