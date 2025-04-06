using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableInstantiator : MonoBehaviour
{
    [Header("Position References")]
    [SerializeField] private Transform cottagePosition;
    [SerializeField] private Transform terrainPosition;
    [SerializeField] private float waitTime = 0.5f; // Wait a second before instantiating

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;

    [Header("Labels")]
    [SerializeField] private string cottageLabel = "Cottage";
    [SerializeField] private string terrainLabel = "Terrain";

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private void Start()
    {
        // Hide the loading panel by default - don't show it until we know we need it
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log($"Loading Panel reference: {(loadingPanel != null ? loadingPanel.name : "NULL")}");
            Debug.Log($"Loading Panel active at start: {(loadingPanel != null ? loadingPanel.activeSelf.ToString() : "N/A")}");
            Debug.Log($"Cottage Position: {(cottagePosition != null ? cottagePosition.name : "NULL")}");
            Debug.Log($"Terrain Position: {(terrainPosition != null ? terrainPosition.name : "NULL")}");
        }

        StartCoroutine(DelayedInstantiation());
    }

    private IEnumerator DelayedInstantiation()
    {
        Debug.Log("Checking if addressables need to be loaded...");
        yield return new WaitForSeconds(waitTime);

        // Check if assets already exist in the scene
        GameObject existingCottage = GameObject.Find("Cottage_Addressable");
        GameObject existingTerrain = GameObject.Find("Terrain_Addressable");

        if (existingCottage != null && existingTerrain != null)
        {
            Debug.Log("Addressable assets already exist in scene - no loading needed");
            // No need to show loading panel at all
            yield break;
        }

        // Assets need to be loaded, show the loading panel now
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (debugMode)
            {
                Debug.Log("Showing loading panel");
                Debug.Log($"Loading Panel active after showing: {loadingPanel.activeSelf}");
            }
            UpdateLoadingUI("Initializing...", 0f);
        }

        // Find the preloader
        var preloaders = FindObjectsByType<BackgroundPreloader>(FindObjectsSortMode.None);
        Debug.Log($"Found {preloaders.Length} BackgroundPreloader instances");

        BackgroundPreloader preloader = null;
        if (preloaders.Length > 0)
        {
            foreach (var p in preloaders)
            {
                Debug.Log($"Preloader instance: {p.gameObject.name}, Parent: {p.transform.parent?.name}");
                preloader = p; // Use the last one found
            }

            Debug.Log("Using preloader: " + preloader.gameObject.name);

            // If preloader is still loading, wait for it
            if (preloader.IsPreloading)
            {
                UpdateLoadingUI("Waiting for preloader to complete...", preloader.PreloadProgress);

                // Subscribe to progress updates
                preloader.OnProgressChanged += UpdateProgress;

                // Wait for it to finish with a timeout
                float timeout = 30f;
                float elapsed = 0f;

                while (preloader.IsPreloading && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Unsubscribe when done
                preloader.OnProgressChanged -= UpdateProgress;

                if (elapsed >= timeout)
                {
                    Debug.LogWarning("Preloader timed out, trying direct loading");
                    StartDirectLoading();
                    yield break;
                }
            }

            // Try to use preloaded assets
            UpdateLoadingUI("Retrieving assets...", 0.8f);

            List<GameObject> cottagePrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(cottageLabel);
            List<GameObject> terrainPrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(terrainLabel);

            bool assetsFound = false;

            // Place cottage
            if (cottagePrefabs.Count > 0 && existingCottage == null && cottagePosition != null)
            {
                GameObject addressableCottage = Instantiate(cottagePrefabs[0]);
                addressableCottage.transform.position = cottagePosition.position;
                addressableCottage.transform.rotation = cottagePosition.rotation;
                addressableCottage.transform.localScale = cottagePosition.localScale;
                addressableCottage.name = "Cottage_Addressable";
                Debug.Log("Cottage instantiated successfully from preloader");
                assetsFound = true;
            }

            // Place terrain
            if (terrainPrefabs.Count > 0 && existingTerrain == null && terrainPosition != null)
            {
                GameObject addressableTerrain = Instantiate(terrainPrefabs[0]);
                addressableTerrain.transform.position = terrainPosition.position;
                addressableTerrain.transform.rotation = terrainPosition.rotation;
                addressableTerrain.transform.localScale = terrainPosition.localScale;
                addressableTerrain.name = "Terrain_Addressable";
                Debug.Log("Terrain instantiated successfully from preloader");
                assetsFound = true;
            }

            if (assetsFound)
            {
                UpdateLoadingUI("Assets loaded successfully", 1.0f);
                yield return new WaitForSeconds(0.5f); // Brief delay before hiding
                HideLoadingPanel();
                yield break;
            }
            else
            {
                Debug.Log("No assets found in preloader, trying direct loading");
                StartDirectLoading();
                yield break;
            }
        }
        else
        {
            Debug.LogError("No BackgroundPreloader found!");
            // Create a preloader if none exists
            GameObject preloaderObj = new GameObject("BackgroundPreloader");
            preloader = preloaderObj.AddComponent<BackgroundPreloader>();

            // Set labels via reflection
            System.Reflection.FieldInfo labelsField = typeof(BackgroundPreloader).GetField("labelsToPreload",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (labelsField != null)
            {
                labelsField.SetValue(preloader, new string[] { cottageLabel, terrainLabel });
            }

            DontDestroyOnLoad(preloaderObj);

            // Start direct loading since we just created the preloader
            StartDirectLoading();
        }
    }

    private void StartDirectLoading()
    {
        Debug.Log("Starting direct loading of addressables");
        StartCoroutine(DirectLoadingProcess());
    }

    private IEnumerator DirectLoadingProcess()
    {
        // Check if assets already exist
        GameObject existingCottage = GameObject.Find("Cottage_Addressable");
        GameObject existingTerrain = GameObject.Find("Terrain_Addressable");

        // Load cottage
        if (existingCottage == null && cottagePosition != null)
        {
            UpdateLoadingUI("Loading cottage...", 0.2f);
            AsyncOperationHandle<IList<GameObject>> cottageOp = default;

            try
            {
                cottageOp = Addressables.LoadAssetsAsync<GameObject>(cottageLabel, null);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (cottageOp.IsValid())
            {
                // Wait for completion (outside try-catch)
                while (!cottageOp.IsDone)
                {
                    UpdateLoadingUI($"Loading cottage... {cottageOp.PercentComplete:P0}", 0.2f + cottageOp.PercentComplete * 0.3f);
                    yield return null;
                }

                try
                {
                    if (cottageOp.Status == AsyncOperationStatus.Succeeded && cottageOp.Result.Count > 0)
                    {
                        GameObject addressableCottage = Instantiate(cottageOp.Result[0]);
                        addressableCottage.transform.position = cottagePosition.position;
                        addressableCottage.transform.rotation = cottagePosition.rotation;
                        addressableCottage.transform.localScale = cottagePosition.localScale;
                        addressableCottage.name = "Cottage_Addressable";
                        Debug.Log("Cottage instantiated successfully from direct load");
                    }
                    else
                    {
                        Debug.LogError($"Failed to load cottage: {cottageOp.OperationException}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.LogError("Failed to start cottage loading operation");
            }
        }

        // Load terrain
        if (existingTerrain == null && terrainPosition != null)
        {
            UpdateLoadingUI("Loading terrain...", 0.6f);
            AsyncOperationHandle<IList<GameObject>> terrainOp = default;

            try
            {
                terrainOp = Addressables.LoadAssetsAsync<GameObject>(terrainLabel, null);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (terrainOp.IsValid())
            {
                // Wait for completion (outside try-catch)
                while (!terrainOp.IsDone)
                {
                    UpdateLoadingUI($"Loading terrain... {terrainOp.PercentComplete:P0}", 0.6f + terrainOp.PercentComplete * 0.3f);
                    yield return null;
                }

                try
                {
                    if (terrainOp.Status == AsyncOperationStatus.Succeeded && terrainOp.Result.Count > 0)
                    {
                        GameObject addressableTerrain = Instantiate(terrainOp.Result[0]);
                        addressableTerrain.transform.position = terrainPosition.position;
                        addressableTerrain.transform.rotation = terrainPosition.rotation;
                        addressableTerrain.transform.localScale = terrainPosition.localScale;
                        addressableTerrain.name = "Terrain_Addressable";
                        Debug.Log("Terrain instantiated successfully from direct load");
                    }
                    else
                    {
                        Debug.LogError($"Failed to load terrain: {terrainOp.OperationException}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.LogError("Failed to start terrain loading operation");
            }
        }

        // Operation complete
        UpdateLoadingUI("Assets loading complete", 1.0f);
        yield return new WaitForSeconds(0.5f);
        HideLoadingPanel();

        // Double-check after a short delay to ensure any animation completed
        yield return new WaitForSeconds(0.5f);
        ForceHideLoadingPanel();
    }

    private void UpdateProgress(float progress)
    {
        UpdateLoadingUI("Loading assets...", progress);
    }

    private void UpdateLoadingUI(string status, float progress)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        Debug.Log($"Loading status: {status} ({progress:P0})");
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            Debug.Log("Hiding loading panel");
            loadingPanel.SetActive(false);

            // For world space canvas in VR, also try these approaches
            CanvasGroup canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            Canvas canvas = loadingPanel.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            if (debugMode)
            {
                Debug.Log($"Loading Panel active after hide attempt: {loadingPanel.activeSelf}");
            }
        }
    }

    private void ForceHideLoadingPanel()
    {
        if (loadingPanel != null && loadingPanel.activeSelf)
        {
            Debug.LogWarning("Loading panel still active - forcing it to hide");
            loadingPanel.SetActive(false);

            // Try setting all parent canvases to inactive as well if the panel is nested
            Transform parent = loadingPanel.transform.parent;
            while (parent != null)
            {
                Canvas parentCanvas = parent.GetComponent<Canvas>();
                if (parentCanvas != null && parentCanvas.gameObject.name.Contains("Loading"))
                {
                    Debug.Log("Also hiding parent loading canvas: " + parentCanvas.gameObject.name);
                    parentCanvas.gameObject.SetActive(false);
                }
                parent = parent.parent;
            }
        }
    }

    private void OnDestroy()
    {
        // Ensure we clean up any event subscriptions
        var preloader = FindFirstObjectByType<BackgroundPreloader>();
        if (preloader != null)
        {
            preloader.OnProgressChanged -= UpdateProgress;
        }
    }

    // Public test method that can be called from a UI button
    public void TestHidePanel()
    {
        Debug.Log("Manual hide panel triggered");
        HideLoadingPanel();
        ForceHideLoadingPanel();
    }
}