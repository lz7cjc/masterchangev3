using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Collections.Generic;

public class UpdatedFilmvotes2 : MonoBehaviour
{
    [Header("API Endpoints")]
    [SerializeField] private string posturllive = "https://masterchange.today/php_scripts/setgetriros.php";
    [SerializeField] private string recordtip = "https://masterchange.today/php_scripts/filmvote.php";

    [Header("UI References")]
    public GameObject tipsPanel;
    public GameObject[] tipButtons;

    [Header("Scene Transition")]
    [SerializeField] private bool useAdditiveSceneLoading = true;
    [SerializeField] private bool debugMode = true;

    private bool mousehover = false;
    private float Counter = 0;
    private float waitFor = 3.0f;
    private int rirosPaid = 0;
    private bool processingTip = false;

    private int userid;
    private string creditURL;
    private int rirosSpent;
    private int rirosBalance;
    private int rirosBought;
    private int rirosEarnt;

    void Start()
    {
        creditURL = PlayerPrefs.GetString("VideoUrl");

        if (PlayerPrefs.HasKey("dbuserid"))
        {
            userid = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("User ID from player prefs: " + userid);
        }
        else
        {
            userid = 0;
        }

        rirosBalance = PlayerPrefs.GetInt("rirosBalance", 0);
        rirosBought = PlayerPrefs.GetInt("rirosBought", 0);
        rirosSpent = PlayerPrefs.GetInt("rirosSpent", 0);
        rirosEarnt = PlayerPrefs.GetInt("rirosEarnt", 0);
    }

    void Update()
    {
        if (mousehover && !processingTip)
        {
            Counter += Time.deltaTime;

            if (Counter >= waitFor)
            {
                mousehover = false;
                Counter = 0;

                // When the timer completes, process the tip
                if (!processingTip)
                {
                    processingTip = true;
                    StartCoroutine(ProcessTip());
                }
            }
        }
    }

    IEnumerator ProcessTip()
    {
        creditURL = PlayerPrefs.GetString("VideoUrl");
        Debug.Log("Processing tip of R$" + rirosPaid + " for video: " + creditURL);

        // Create tip form data
        WWWForm formTips = new WWWForm();
        formTips.AddField("voteis", rirosPaid);
        formTips.AddField("filmid", creditURL);
        formTips.AddField("userid", userid);

        // Send the tip data
        using (UnityWebRequest www = UnityWebRequest.Post(recordtip, formTips))
        {
            www.timeout = 5; // Set a timeout to prevent hanging
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error recording tip: " + www.error);
            }
            else
            {
                Debug.Log("Film vote recorded successfully");
            }
        }

        // Process Riros transaction in the background
        StartCoroutine(ProcessRirosTransaction());

        // Return to mainVR scene using the improved approach
        ReturnToMainScene();
    }

    IEnumerator ProcessRirosTransaction()
    {
        if (!PlayerPrefs.HasKey("dbuserid"))
        {
            // Update local values if no user ID
            PlayerPrefs.SetInt("rirosSpent", rirosSpent + rirosPaid);
            PlayerPrefs.SetInt("rirosBalance", rirosBalance - rirosPaid);
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("userid", userid);
        form.AddField("rirosValue", rirosPaid);
        form.AddField("description", "Film Tip: " + creditURL);
        form.AddField("riroType", "Spent");

        using (UnityWebRequest www = UnityWebRequest.Post(posturllive, form))
        {
            www.timeout = 5; // Set a timeout to prevent hanging
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error processing Riros: " + www.error);
                // Update local values as fallback
                PlayerPrefs.SetInt("rirosSpent", rirosSpent + rirosPaid);
                PlayerPrefs.SetInt("rirosBalance", rirosBalance - rirosPaid);
            }
            else
            {
                string json = www.downloadHandler.text;

                try
                {
                    riros riros = JsonUtility.FromJson<riros>(json);
                    if (riros != null)
                    {
                        PlayerPrefs.SetInt("rirosEarnt", riros.Earnt);
                        PlayerPrefs.SetInt("rirosBought", riros.Bought);
                        PlayerPrefs.SetInt("rirosSpent", riros.Spent);
                        PlayerPrefs.SetInt("rirosBalance", riros.Bought + riros.Earnt - riros.Spent);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error parsing Riros data: " + ex.Message);
                    // Update local values as fallback
                    PlayerPrefs.SetInt("rirosSpent", rirosSpent + rirosPaid);
                    PlayerPrefs.SetInt("rirosBalance", rirosBalance - rirosPaid);
                }
            }
        }
    }

    private void ReturnToMainScene()
    {
        // Check if we should use the additive scene loading approach
        if (useAdditiveSceneLoading && PlayerPrefs.GetInt("comingFromMainVR", 0) == 1)
        {
            if (debugMode)
            {
                Debug.Log("Using additive scene transition to return to mainVR");
            }

            // Start the coroutine to handle the scene transition
            StartCoroutine(ReturnToMainVRAdditively());
        }
        else
        {
            // Fall back to the simple approach
            if (debugMode)
            {
                Debug.Log("Using standard scene loading to return to mainVR");
            }

            SceneManager.LoadScene("mainVR");
        }
    }

    private IEnumerator ReturnToMainVRAdditively()
    {
        // First find the mainVR scene
        bool foundMainVR = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name.ToLower() == "mainvr")
            {
                foundMainVR = true;

                if (debugMode)
                {
                    Debug.Log("Found mainVR scene, reactivating components");
                }

                // Get the list of objects to reactivate
                List<string> objectsToReactivate = GetReactivationList();

                // Get all root GameObjects in the scene
                GameObject[] rootObjects = scene.GetRootGameObjects();

                // Reactivate all root objects in mainVR that were previously deactivated
                foreach (GameObject root in rootObjects)
                {
                    if (objectsToReactivate.Contains(root.name))
                    {
                        root.SetActive(true);

                        if (debugMode)
                        {
                            Debug.Log($"Reactivated mainVR GameObject: {root.name}");
                        }
                    }
                }

                // Set mainVR as active scene
                SceneManager.SetActiveScene(scene);

                // Clear the reactivation list
                ClearReactivationList();

                break;
            }
        }

        if (!foundMainVR)
        {
            Debug.LogError("Could not find mainVR scene! Loading it directly instead.");
            SceneManager.LoadScene("mainVR");
            yield break;
        }

        // Get current scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Only unload if it's not mainVR (avoid unloading the scene we just activated)
        if (currentScene.name.ToLower() != "mainvr")
        {
            if (debugMode)
            {
                Debug.Log($"Unloading scene: {currentScene.name}");
            }

            // Unload the current scene
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }

        // Reset PlayerPrefs
        PlayerPrefs.SetInt("comingFromMainVR", 0);
    }

    // Get the list of object names to reactivate
    private List<string> GetReactivationList()
    {
        List<string> result = new List<string>();
        string listStr = PlayerPrefs.GetString("mainvr_reactivate", "");

        if (!string.IsNullOrEmpty(listStr))
        {
            string[] items = listStr.Split('|');
            result.AddRange(items);
        }

        return result;
    }

    // Clear the reactivation list
    private void ClearReactivationList()
    {
        PlayerPrefs.DeleteKey("mainvr_reactivate");
    }

    // Method called by PointerEnter event
    public void MouseHoverChangeScene(int amount)
    {
        Debug.Log("Setting tip amount: R$" + amount);
        rirosPaid = amount;

        // Reset and start the hover timer
        mousehover = true;
        Counter = 0;
    }

    // Method called by PointerExit event
    public void MouseExit()
    {
        Debug.Log("Cancelling tip selection");
        mousehover = false;
        Counter = 0;
    }

    [System.Serializable]
    private class riros
    {
        public int Bought;
        public int Spent;
        public int Earnt;
    }
}