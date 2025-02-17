using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class AIConfig
{
    public int maxCharacterLimit = 500; // Default value
    public string apiKey;
    public string modelName = "gpt-3.5-turbo";
}

public class AIContentManager : MonoBehaviour
{
    [SerializeField]
    private AIConfig config;

    [Tooltip("Maximum characters in the AI response")]
    [SerializeField]
    private int characterLimit = 500;

    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    public async Task<string> GetRecommendedContent(string jsonData)
    {
        // Create the request body with character limit
        var requestBody = new
        {
            model = config.modelName,
            max_tokens = config.maxCharacterLimit / 4, // Approximate tokens from characters
            messages = new[]
            {
                new {
                    role = "system",
                    content = $"You are a content recommendation assistant. Provide responses within {config.maxCharacterLimit} characters."
                },
                new {
                    role = "user",
                    content = $"Based on this data, provide relevant content: {jsonData}"
                }
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                    string content = response.choices[0].message.content;

                    // Ensure the response doesn't exceed the character limit
                    if (content.Length > config.maxCharacterLimit)
                    {
                        content = content.Substring(0, config.maxCharacterLimit);
                    }

                    return content;
                }
                else
                {
                    Debug.LogError($"API Error: {request.error}");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Request failed: {e.Message}");
                return null;
            }
        }
    }
}

// Example usage in your questionnaire handler
public class QuestionnaireHandler : MonoBehaviour
{
    [SerializeField]
    private AIContentManager aiManager;

    public async void ProcessQuestionnaireData(Dictionary<string, string> answers)
    {
        // Convert your answers to JSON
        string jsonData = JsonConvert.SerializeObject(answers);

        try
        {
            string recommendedContent = await aiManager.GetRecommendedContent(jsonData);
            if (!string.IsNullOrEmpty(recommendedContent))
            {
                // Handle the content in your app
                DisplayContent(recommendedContent);
            }
            else
            {
                Debug.LogWarning("No content received from AI");
                // Handle the error case
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing questionnaire: {e.Message}");
            // Handle the error
        }
    }

    private void DisplayContent(string content)
    {
        // Your content display logic here
    }
}

// Define the response classes
public class OpenAIResponse
{
    public List<Choice> choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
}

public class Message
{
    public string content { get; set; }
}
