using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AITips : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://masterchange.today/php_scripts/ai/aigeneratecontent.php";
    [SerializeField] private int contentLength = 100;
    [SerializeField] private TextMeshPro resultText;

    private int userId;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            userId = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("[AITips] Retrieved userId from PlayerPrefs: " + userId);
            StartCoroutine(RunEndToEndTest());
        }
        else
        {
            resultText.text = "To see personalised tips based on your lifestyle, goals and challenges, log in or register. Go to your dashboard via the HUD";
        }
    }

    private IEnumerator RunEndToEndTest()
    {
        // Create the configuration object
        var config = new Config
        {
            userid = userId,
            contentLength = contentLength
        };

        // Debug log to inspect the configuration object
        Debug.Log("[AITips] Configuration Object: " + JsonUtility.ToJson(config, true));

        // Convert the configuration object to JSON
        string jsonConfig = JsonUtility.ToJson(config);
        Debug.Log("[AITips] JSON Payload: " + jsonConfig);

        // Create a UnityWebRequest to send the POST request
        UnityWebRequest request = UnityWebRequest.PostWwwForm(apiUrl, jsonConfig);
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
            // Display the plain text response
            string responseText = request.downloadHandler.text;
            Debug.Log("[AITips] Response received: " + responseText);
            resultText.text = responseText;
        }
    }

    [System.Serializable]
    private class Config
    {
        public int userid;
        public int contentLength;
    }
}
