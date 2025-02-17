using UnityEngine;
using UnityEngine.Video;

public class VideoLoader : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    void Start()
    {
        Debug.Log("videoplayer url 1");
        string url = PlayerPrefs.GetString("videourl", "");
        Debug.Log("videoplayer url 2" + url);
        if (!string.IsNullOrEmpty(url))
        {
            Debug.Log("videoplayer url 3" + url);
            videoPlayer.url = url;
            Debug.Log("videoplayer url 4" + url);
            videoPlayer.Play();
        }
    }

    public void SaveVideoURL(string url)
    {
        Debug.Log("videoplayer url 5" + url);
        PlayerPrefs.SetString("videourl", url);
        Debug.Log("videoplayer url 6" + url);
        PlayerPrefs.Save();
    }
}