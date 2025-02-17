using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class AITipsManager : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://masterchange.today/php_scripts/ai/aigeneratecontent.php";
    [SerializeField] private int contentLength = 100;
    [SerializeField] private int numTips = 5;
    [SerializeField] private TextMeshPro resultText;
    [SerializeField] private AssetReference tipPrefab;

    private int userId;
    private List<string> tips = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            userId = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("[AITipsManager] Retrieved userId from PlayerPrefs: " + userId);
            StartCoroutine(GenerateTips());
        }
        else
        {
            resultText.text = "To see personalised tips based on your lifestyle, goals and challenges, log in or register. Go to your dashboard via the HUD";
        }
    }

    private IEnumerator GenerateTips()
    {
        // Create the configuration object
        var config = new Config
        {
            userid = userId,
            contentLength = contentLength,
            numTips = numTips
        };

        // Convert the configuration object to JSON
        string jsonConfig = JsonUtility.ToJson(config);
        Debug.Log("[AITipsManager] JSON Payload: " + jsonConfig);

        // Create a UnityWebRequest to send the POST request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonConfig);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        Debug.Log("[AITipsManager] Response Code: " + request.responseCode);
        Debug.Log("[AITipsManager] Response Text: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("[AITipsManager] Error: " + request.error);
        }
        else
        {
            // Parse the JSON response
            var response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
            tips = new List<string>(response.tips);
            Debug.Log("[AITipsManager] Tips received: " + string.Join(", ", tips));

            // Store tips as addressables
            StoreTipsAsAddressables();
        }
    }

    private void StoreTipsAsAddressables()
    {
        foreach (var tip in tips)
        {
            Addressables.InstantiateAsync(tipPrefab).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var tipInstance = handle.Result;
                    var textMeshPro = tipInstance.GetComponentInChildren<TextMeshPro>();
                    if (textMeshPro != null)
                    {
                        textMeshPro.text = tip;
                    }
                }
                else
                {
                    Debug.LogError("[AITipsManager] Failed to instantiate tip prefab.");
                }
            };
        }
    }

    [System.Serializable]
    private class Config
    {
        public int userid;
        public int contentLength;
        public int numTips;
    }

    [System.Serializable]
    private class APIResponse
    {
        public List<string> tips;
    }
}
