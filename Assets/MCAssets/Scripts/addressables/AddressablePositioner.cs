using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablePositioner : MonoBehaviour
{
    [SerializeField] private string cottageLabelName = "Cottage";
    [SerializeField] private string terrainLabelName = "Terrain";

    [Header("Position References")]
    [SerializeField] private Transform cottagePosition;
    [SerializeField] private Transform terrainPosition;

    private void Start()
    {
        StartCoroutine(LoadAndPositionAddressables());
    }

    private IEnumerator LoadAndPositionAddressables()
    {
        BackgroundPreloader preloader = BackgroundPreloader.Instance;

        if (preloader == null)
        {
            Debug.LogError("BackgroundPreloader not found. Make sure it's in your scene.");
            yield break;
        }

        // Wait for preloader to finish
        while (!preloader.PreloadingComplete)
        {
            yield return null;
        }

        // Position cottage
        List<GameObject> cottagePrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(cottageLabelName);
        if (cottagePrefabs.Count > 0 && cottagePosition != null)
        {
            GameObject addressableCottage = Instantiate(cottagePrefabs[0]);

            // Match position, rotation and scale of reference transform
            addressableCottage.transform.position = cottagePosition.position;
            addressableCottage.transform.rotation = cottagePosition.rotation;
            addressableCottage.transform.localScale = cottagePosition.localScale;

            // Rename for clarity
            addressableCottage.name = "Cottage_Addressable";

            Debug.Log("Positioned addressable cottage at reference position");
        }

        // Position terrain
        List<GameObject> terrainPrefabs = preloader.GetLoadedAssetsByLabel<GameObject>(terrainLabelName);
        if (terrainPrefabs.Count > 0 && terrainPosition != null)
        {
            GameObject addressableTerrain = Instantiate(terrainPrefabs[0]);

            // Match position, rotation and scale of reference transform
            addressableTerrain.transform.position = terrainPosition.position;
            addressableTerrain.transform.rotation = terrainPosition.rotation;
            addressableTerrain.transform.localScale = terrainPosition.localScale;

            // Rename for clarity
            addressableTerrain.name = "Terrain_Addressable";

            Debug.Log("Positioned addressable terrain at reference position");
        }
    }
}