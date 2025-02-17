using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.Networking;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;

public class ContentManager : MonoBehaviour
{
    public TMP_Text ContentBody;
    public int contenttype;
    [SerializeField] private string apiKey;

    private int dbuserid;
    string aiEndpoint = "https://masterchange.today/php_scripts/aigeneratecontent.php";
    string originalEndpoint = "https://masterchange.today/php_scripts/allmytips.php";

    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            dbuserid = PlayerPrefs.GetInt("dbuserid");
            CallRegisterCoroutine();
        }
        else
        {
            ContentBody.text = "Reserved for personalised content...";
        }
    }

    public void CallRegisterCoroutine()
    {
        StartCoroutine(GetContent());
    }

    IEnumerator GetContent()
    {
        // First try AI generation
        WWWForm aiForm = new WWWForm();
        aiForm.AddField("contenttype", contenttype);
        aiForm.AddField("userid", dbuserid);

        using (UnityWebRequest aiRequest = UnityWebRequest.Post(aiEndpoint, aiForm))
        {
            yield return aiRequest.SendWebRequest();

            if (aiRequest.result == UnityWebRequest.Result.Success && aiRequest.downloadHandler.text != "0 results")
            {
                PlayerTipsJSON aiContent = JsonConvert.DeserializeObject<PlayerTipsJSON>(aiRequest.downloadHandler.text);
                if (aiContent.data != null && aiContent.data.Count > 0)
                {
                    ContentBody.text = aiContent.data[0].ContentBody;
                    yield break;
                }
            }

            // If AI fails or returns no results, fall back to original script
            WWWForm originalForm = new WWWForm();
            originalForm.AddField("contenttype", contenttype);
            originalForm.AddField("userid", dbuserid);
            originalForm.AddField("title", 0);

            using (UnityWebRequest originalRequest = UnityWebRequest.Post(originalEndpoint, originalForm))
            {
                yield return originalRequest.SendWebRequest();

                if (originalRequest.result == UnityWebRequest.Result.Success)
                {
                    string json = originalRequest.downloadHandler.text;
                    if (json != "0 results")
                    {
                        PlayerTipsJSON loadedPlayerData = JsonConvert.DeserializeObject<PlayerTipsJSON>(json);
                        ContentBody.text = loadedPlayerData.data[0].ContentBody;
                    }
                    else
                    {
                        ContentBody.text = "We only offer you content relevant to you...";
                    }
                }
                else
                {
                    ContentBody.text = originalRequest.error;
                }
            }
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string ContentBody;
    }

    [Serializable]
    public class PlayerTipsJSON
    {
        public List<PlayerData> data;
    }
}
