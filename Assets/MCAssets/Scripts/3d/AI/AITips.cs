using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AITips : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://masterchange.today/php_scripts/ai/aigeneratecontent.php";
    [SerializeField] private int baseContentLength = 100;
    [SerializeField] private TextMeshPro resultText;
    [SerializeField] private AssetReference tipboardPrefab;

    private int userId;
    private List<GameObject> tipboards = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            userId = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("[AITips] Retrieved userId from PlayerPrefs: " + userId);
            StartCoroutine(GenerateAndPopulateTipboards());
        }
        else
        {
            resultText.text = "To see personalised tips based on your lifestyle, goals and challenges, log in or register. Go to your dashboard via the HUD";
        }
    }

    private IEnumerator GenerateAndPopulateTipboards()
    {
        // Find all existing tipboards in the scene
        tipboards.AddRange(GameObject.FindGameObjectsWithTag("TipBoard"));
        Debug.Log("[AITips] Found " + tipboards.Count + " tipboards in the scene.");

        // Generate a list of unique content lengths
        List<int> contentLengths = new List<int>();
        for (int i = 0; i < tipboards.Count; i++)
        {
            contentLengths.Add(baseContentLength + i * 10); // Example: increment content length by 10 for each tipboard
        }

        // Create the configuration object
        var config = new Config
        {
            userid = userId,
            contentLengths = contentLengths,
            numTips = tipboards.Count
        };

        // Debug log to inspect the configuration object
        Debug.Log("[AITips] Configuration Object: " + JsonUtility.ToJson(config, true));

        // Convert the configuration object to JSON
        string jsonConfig = JsonUtility.ToJson(config);
        Debug.Log("[AITips] JSON Payload: " + jsonConfig);

        // Create a UnityWebRequest to send the POST request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonConfig);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        Debug.Log("[AITips] Response Code: " + request.responseCode);
        Debug.Log("[AITips] Response Text: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("[AITips] Error: " + request.error);
        }
        else
        {
            // Parse the JSON response
            var response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
            List<string> tips = response.tips;
            List<string> prompts = response.prompts;
            Debug.Log("[AITips] Tips received: " + string.Join(", ", tips));

            // Log the prompt for each tip received
            for (int i = 0; i < prompts.Count; i++)
            {
                Debug.Log("[AITips] APIPrompt: " + prompts[i]);
            }

            // Populate existing tipboards with tips
            for (int i = 0; i < tipboards.Count && i < tips.Count; i++)
            {
                var textMeshPro = tipboards[i].GetComponentInChildren<TextMeshPro>();
                if (textMeshPro != null)
                {
                    textMeshPro.text = tips[i];
                }
                else
                {
                    Debug.LogError("[AITips] No TextMeshPro component found in tipboard: " + tipboards[i].name);
                }
            }
        }
    }

    [System.Serializable]
    private class Config
    {
        public int userid;
        public List<int> contentLengths;
        public int numTips;
    }

    [System.Serializable]
    private class APIResponse
    {
        public List<string> tips;
        public List<string> prompts;
    }
}
