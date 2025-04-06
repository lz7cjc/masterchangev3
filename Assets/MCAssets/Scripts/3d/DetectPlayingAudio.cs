using UnityEngine;
using System.Collections.Generic;

public class DetectPlayingAudio : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Time between scans for playing audio")]
    public float scanInterval = 1.0f;

    private float nextScanTime;
    private Dictionary<AudioSource, GameObject> previouslyPlayingSources = new Dictionary<AudioSource, GameObject>();

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            ScanForPlayingAudio();
            nextScanTime = Time.time + scanInterval;
        }
    }

    private void ScanForPlayingAudio()
    {
        // Get all audio sources in the scene (including inactive GameObjects)
        AudioSource[] allSources = Resources.FindObjectsOfTypeAll<AudioSource>();
        Dictionary<AudioSource, GameObject> currentlyPlayingSources = new Dictionary<AudioSource, GameObject>();

        foreach (AudioSource source in allSources)
        {
            if (source.isPlaying)
            {
                currentlyPlayingSources[source] = source.gameObject;

                // If this is newly playing, log it
                if (!previouslyPlayingSources.ContainsKey(source))
                {
                    string clipName = source.clip != null ? source.clip.name : "Unknown Clip";
                    string path = GetGameObjectPath(source.gameObject);
                    Debug.Log($"Playing Audio Detected - GameObject: {path}, Clip: {clipName}, Volume: {source.volume}");
                }
            }
        }

        // Check for sources that have stopped playing
        foreach (var kvp in previouslyPlayingSources)
        {
            if (!currentlyPlayingSources.ContainsKey(kvp.Key))
            {
                string clipName = kvp.Key.clip != null ? kvp.Key.clip.name : "Unknown Clip";
                string path = GetGameObjectPath(kvp.Value);
                Debug.Log($"Audio Stopped - GameObject: {path}, Clip: {clipName}");
            }
        }

        previouslyPlayingSources = currentlyPlayingSources;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}