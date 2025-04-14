using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioDebugger : MonoBehaviour
{
    void Start()
    {
        // Find all AudioSources in the scene and DontDestroyOnLoad space
        AudioSource[] allAudioSources = Object.FindObjectsByType<AudioSource>(
            FindObjectsSortMode.None // No sorting needed, it's faster
        );

        foreach (AudioSource source in allAudioSources)
        {
            if (source.isPlaying)
            {
                Debug.Log($"🎵 Audio playing on: {source.gameObject.name}, Clip: {source.clip?.name}", source.gameObject);
            }
        }
    }
}
