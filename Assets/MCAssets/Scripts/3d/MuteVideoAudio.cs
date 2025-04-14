using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class MuteVideoAudio : MonoBehaviour
{
    void Awake()
    {
        var videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null && videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            // Make sure we're controlling the track
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetDirectAudioMute(0, true);   // This is the key line!
        }
    }
}
