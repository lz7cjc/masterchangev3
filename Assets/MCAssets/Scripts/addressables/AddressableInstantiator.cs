using System.Collections;
using UnityEngine;

public class AddressableInstantiator : MonoBehaviour
{
    [SerializeField] private Vector3 cottagePosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 terrainPosition = new Vector3(0, 0, 0);
    [SerializeField] private float waitTime = 1f; // Wait a second before instantiating

    private void Start()
    {
        StartCoroutine(DelayedInstantiation());
    }

    private IEnumerator DelayedInstantiation()
    {
        Debug.Log("Waiting for addressables to be ready...");
        yield return new WaitForSeconds(waitTime);

        // Find the preloader
        var preloaders = FindObjectsOfType<BackgroundPreloader>(true);
        Debug.Log($"Found {preloaders.Length} BackgroundPreloader instances");

        BackgroundPreloader preloader = null;
        foreach (var p in preloaders)
        {
            Debug.Log($"Preloader instance: {p.gameObject.name}, Parent: {p.transform.parent?.name}");
            preloader = p; // Use the last one found
        }

        if (preloader != null)
        {
            Debug.Log("Using preloader: " + preloader.gameObject.name);

            // Try direct instantiation from Addressables
            Debug.Log("Attempting direct instantiation of Cottage");
            var cottage = preloader.GetLoadedAssetsByLabel<GameObject>("Cottage");
            if (cottage.Count > 0)
            {
                var instance = Instantiate(cottage[0], cottagePosition, Quaternion.identity);
                instance.name = "Cottage_Instantiated";
                Debug.Log("Cottage instantiated successfully");
            }
            else
            {
                Debug.LogError("No cottage prefabs found!");
            }

            Debug.Log("Attempting direct instantiation of Terrain");
            var terrain = preloader.GetLoadedAssetsByLabel<GameObject>("Terrain");
            if (terrain.Count > 0)
            {
                var instance = Instantiate(terrain[0], terrainPosition, Quaternion.identity);
                instance.name = "Terrain_Instantiated";
                Debug.Log("Terrain instantiated successfully");
            }
            else
            {
                Debug.LogError("No terrain prefabs found!");
            }
        }
        else
        {
            Debug.LogError("No BackgroundPreloader found!");
        }
    }
}