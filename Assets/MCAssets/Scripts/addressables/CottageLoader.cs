using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
public class CottageLoader : MonoBehaviour
{
    [SerializeField]
    private Vector3 spawnPosition = Vector3.zero;

    private AsyncOperationHandle<GameObject> cottageHandle;

    private void Start()
    {
        LoadWater();
    }

    private async void LoadWater()
    {
        await Task.Delay(2000); // Add 100ms delay
        Debug.Log("Starting water load...");
        var startTime = Time.realtimeSinceStartup;

        cottageHandle = Addressables.LoadAssetAsync<GameObject>("Assets/3rdPartyAssets/StylizedWater2/Prefabs/StylizedWater_Lowpoly.prefab");
        var waterPrefab = await cottageHandle.Task;

        Debug.Log($"Water loaded in {Time.realtimeSinceStartup - startTime} seconds");
        Instantiate(waterPrefab, spawnPosition, Quaternion.identity);
    }

    private void OnDestroy()
    {
        if (cottageHandle.IsValid())
        {
            Addressables.Release(cottageHandle);
        }
    }
}