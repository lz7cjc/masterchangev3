using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddressablePositioner : MonoBehaviour
{
    [SerializeField] private string cottageLabelName = "Cottage";
    [SerializeField] private string terrainLabelName = "Terrain";

    [Header("Position References")]
    [SerializeField] private Transform cottagePosition;
    [SerializeField] private Transform terrainPosition;

    [Header("Options")]
    [SerializeField] private float initialDelaySeconds = 0.5f;
    [SerializeField] private bool ensurePreloaderExists = true;

    private void Start()
    {
        // Small delay to ensure everything is initialized
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(initialDelaySeconds);

        // Check if assets are already in scene
        GameObject existingCottage = GameObject.Find("Cottage_Addressable");
        GameObject existingTerrain = GameObject.Find("Terrain_Addressable");

        if (existingCottage != null && existingTerrain != null)
        {
            Debug.Log("Addressable assets already exist in scene");
            yield break;
        }

        // Find or create preloader
        BackgroundPreloader preloader = BackgroundPreloader.Instance;

        if (preloader == null && ensurePreloaderExists)
        {
            Debug.Log("Creating preloader since none exists");
            GameObject preloaderObj = new GameObject("BackgroundPreloader");
            preloader = preloaderObj.AddComponent<BackgroundPreloader>();

            // Set the labels
            System.Reflection.FieldInfo labelsField = typeof(BackgroundPreloader).GetField("labelsToPreload",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (labelsField != null)
            {
                labelsField.SetValue(preloader, new string[] { cottageLabelName, terrainLabelName });
            }

            DontDestroyOnLoad(preloaderObj);

            // Wait for it to initialize
            yield return null;
        }

        if (preloader == null)
        {
            Debug.LogError("BackgroundPreloader not found and could not be created");
            yield break;
        }

        Debug.Log("Found preloader, waiting for completion");

        // Wait for preloader to finish if it's still working
        if (!preloader.PreloadingComplete)
        {
            while (!preloader.PreloadingComplete)
            {
                Debug.Log($"Waiting for preloader... Progress: {preloader.PreloadProgress:P0}");
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("Preloader complete, checking for assets");

        // Get and position assets
        if (existingCottage == null && cottagePosition != null)
        {
            List<GameObject> cottagePrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(cottageLabelName);
            Debug.Log($"Found {cottagePrefabs.Count} cottage prefabs");

            if (cottagePrefabs.Count > 0)
            {
                GameObject addressableCottage = Instantiate(cottagePrefabs[0]);
                addressableCottage.transform.position = cottagePosition.position;
                addressableCottage.transform.rotation = cottagePosition.rotation;
                addressableCottage.transform.localScale = cottagePosition.localScale;
                addressableCottage.name = "Cottage_Addressable";
                Debug.Log("Cottage instantiated successfully");
            }
            else
            {
                Debug.LogError($"No cottage prefabs found with label: {cottageLabelName}");
            }
        }

        if (existingTerrain == null && terrainPosition != null)
        {
            List<GameObject> terrainPrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(terrainLabelName);
            Debug.Log($"Found {terrainPrefabs.Count} terrain prefabs");

            if (terrainPrefabs.Count > 0)
            {
                GameObject addressableTerrain = Instantiate(terrainPrefabs[0]);
                addressableTerrain.transform.position = terrainPosition.position;
                addressableTerrain.transform.rotation = terrainPosition.rotation;
                addressableTerrain.transform.localScale = terrainPosition.localScale;
                addressableTerrain.name = "Terrain_Addressable";
                Debug.Log("Terrain instantiated successfully");
            }
            else
            {
                Debug.LogError($"No terrain prefabs found with label: {terrainLabelName}");
            }
        }
    }
}