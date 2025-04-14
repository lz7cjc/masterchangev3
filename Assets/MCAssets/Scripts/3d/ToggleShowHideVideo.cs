using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ToggleShowHideVideo : MonoBehaviour
{
    public bool mouseHover = false;
    public float counter = 0;
    public string VideoUrlLink;
    private int riroAmount;

    public string returntoscene;
    public string nextscene;
    public int returnstage;
    public string behaviour;
    public TMP_Text TMP_title;
    public bool hasText = true;
    private GameObject cameraTarget;
    [SerializeField] private StartUp StartUp;
    [SerializeField] private RiroStopGoV2 riroStopGoV2;
    private Rigidbody player;

    // New property to store the category (for zone placement)
    [HideInInspector]
    public string category;

    // New property to support zone-based placement
    [HideInInspector]
    public string zoneName;

    public void Start()
    {
        riroAmount = PlayerPrefs.GetInt("rirosBalance");
        // Initialize hasText based on whether TMP_title is assigned
        hasText = (TMP_title != null);

        // Try to extract category from URL if not already set
        if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(VideoUrlLink))
        {
            ExtractCategoryFromUrl();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseHover)
        {
            counter += Time.deltaTime;

            if (counter >= 3)
            {
                mouseHover = false;
                counter = 0;
                SetVideoUrl();
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        if (hasText && TMP_title != null)
        {
            TMP_title.color = Color.green;
        }
        mouseHover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        if (hasText && TMP_title != null)
        {
            TMP_title.color = Color.white;
        }
        mouseHover = false;
        counter = 0;
    }

    // Try to extract category from the URL
    private void ExtractCategoryFromUrl()
    {
        if (string.IsNullOrEmpty(VideoUrlLink)) return;

        string[] parts = VideoUrlLink.Split('/');
        if (parts.Length >= 3)
        {
            category = parts[parts.Length - 3] + "/" + parts[parts.Length - 2];
        }
    }

    public void SetVideoUrl()
    {
        PlayerPrefs.SetString("returntoscene", returntoscene);
        PlayerPrefs.SetString("behaviour", behaviour);
        PlayerPrefs.SetInt("stage", returnstage);
        PlayerPrefs.SetString("nextscene", nextscene);

        if (riroAmount >= 50)
        {
            PlayerPrefs.DeleteKey("stopFilm");
            Debug.Log("eeeee" + riroAmount);
            PlayerPrefs.SetString("VideoUrl", VideoUrlLink);
            SceneManager.LoadScene("360VideoApp");
        }
        else
        {
            Debug.Log("111eeeee" + riroAmount);
            PlayerPrefs.SetInt("stopFilm", 0);
            riroStopGoV2.doNotPass(0);
        }
    }
}