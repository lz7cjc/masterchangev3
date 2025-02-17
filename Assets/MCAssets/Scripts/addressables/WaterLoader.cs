using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WaterLoader : MonoBehaviour
{
    [SerializeField]
    private Vector3 spawnPosition = Vector3.zero;

    private AsyncOperationHandle<GameObject> waterHandle;

    private void Start()
    {
        LoadWater();
    }

    private async void LoadWater()
    {
        Debug.Log("Starting water load...");
        var startTime = Time.realtimeSinceStartup;

        waterHandle = Addressables.LoadAssetAsync<GameObject>("Assets/3rdPartyAssets/StylizedWater2/Prefabs/StylizedWater_Lowpoly.prefab");
        var waterPrefab = await waterHandle.Task;

        Debug.Log($"Water loaded in {Time.realtimeSinceStartup - startTime} seconds");
        Instantiate(waterPrefab, spawnPosition, Quaternion.identity);
    }

    private void OnDestroy()
    {
        if (waterHandle.IsValid())
        {
            Addressables.Release(waterHandle);
        }
    }
}