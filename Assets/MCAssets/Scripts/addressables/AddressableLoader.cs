using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    public string cottageLabel = "Assets/MCAssets/Prefabs/Addressables/MCCottagePrefab.prefab";
    public string terrainLabel = "Assets/MCAssets/Prefabs/Addressables/Terrain.prefab";

    void Start()
    {
        LoadPrefab(cottageLabel);
        LoadPrefab(terrainLabel);
    }

    void LoadPrefab(string label)
    {
        Addressables.LoadAssetAsync<GameObject>(label).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject instance = Instantiate(handle.Result);
                AssignMaterials(instance);
                Debug.Log($"Loaded: {label}");
            }
            else
            {
                Debug.LogError($"Failed to load {label}");
            }
        };
    }

    void AssignMaterials(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterial == null)
            {
                // Assign a default material or log an error
                renderer.sharedMaterial = Resources.Load<Material>("DefaultMaterial");
                Debug.LogWarning($"Assigned default material to {renderer.gameObject.name}");
            }
        }
    }
}
