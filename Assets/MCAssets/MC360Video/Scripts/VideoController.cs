using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TMP_Text videoLengthText;
    public TMP_Text videoProgressText;

    // References to TogglePlayPauseIcons for play/pause button
    public TogglePlayPauseIcons playPauseToggle;
    // References to ToggleActiveIconsVideo for other buttons
    public ToggleActiveIconsVideo stopToggle;
    public ToggleActiveIconsVideo fastForwardToggle;
    public ToggleActiveIconsVideo fFastForwardToggle;
    public ToggleActiveIconsVideo rewindToggle;

    // References to active parts/objects of the scene
    public GameObject[] activeObjects;
    // References to new section/objects to be activated when the film is stopped
    public GameObject[] newSectionObjects;

    public bool mousehover = false;
    public float counter = 0;
    private string videoAction;
    public float delay = 3.0f; // Delay in seconds

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Register the prepareCompleted event
        videoPlayer.prepareCompleted += OnVideoPrepared;

        // Prepare the video
        videoPlayer.Prepare();

        Debug.Log("VideoController Start: VideoPlayer prepared");
    }

    void Update()
    {
        // Update the video progress text
        videoProgressText.text = FormatTime(videoPlayer.time);
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Set the video length text when the video is prepared
        videoLengthText.text = FormatTime(videoPlayer.length);
        // Start the video by default
        videoPlayer.Play();
        playPauseToggle.SetPlaying(true);
        playPauseToggle.PauseIcon(); // Set the play/pause icon to the pause icon
        Debug.Log("OnVideoPrepared: Video length set and playback started");
    }

    public void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            playPauseToggle.SetPlaying(false);
            playPauseToggle.PauseIcon(); // Use the new PauseIcon method
            Debug.Log("TogglePlayPause: Video paused");
        }
        else
        {
            videoPlayer.Play();
            playPauseToggle.SetPlaying(true);
            playPauseToggle.PlayIcon(); // Use the new PlayIcon method
            Debug.Log("TogglePlayPause: Video playing");
        }
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
        stopToggle.SelectIcon();
        Debug.Log("StopVideo: Video stopped");

        // Hide active objects
        foreach (GameObject obj in activeObjects)
        {
            obj.SetActive(false);
        }

        // Show new section objects
        foreach (GameObject obj in newSectionObjects)
        {
            obj.SetActive(true);
        }
    }

    public void FastForward()
    {
        videoPlayer.playbackSpeed = 2.0f;
        fastForwardToggle.SelectIcon();
        Debug.Log("FastForward: Playback speed set to 2.0");
    }

    public void F_FastForward()
    {
        videoPlayer.playbackSpeed = 4.0f;
        fFastForwardToggle.SelectIcon();
        Debug.Log("F_FastForward: Playback speed set to 4.0");
    }

    public void Rewind()
    {
        videoPlayer.time = 0;
        rewindToggle.SelectIcon();
        Debug.Log("Rewind: Video time set to 0");
    }

    public double GetVideoLength()
    {
        return videoPlayer.length;
    }

    public double GetCurrentTime()
    {
        return videoPlayer.time;
    }

    public void SeekToTime(double seconds)
    {
        videoPlayer.time = seconds;
        Debug.Log($"SeekToTime: Video time set to {seconds} seconds");
    }

    private string FormatTime(double time)
    {
        int minutes = Mathf.FloorToInt((float)time / 60F);
        int seconds = Mathf.FloorToInt((float)time - minutes * 60);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    public void MouseHover(string _videoAction)
    {
        Debug.Log($"MouseHover called with action: {_videoAction}");

        // Check if the video is playing or paused and set the appropriate action
        videoAction = _videoAction;

        mousehover = true;
        counter = 0;
        StartCoroutine(HandleMouseHover());
    }

    private IEnumerator HandleMouseHover()
    {
        Debug.Log("HandleMouseHover started");
        yield return new WaitForSeconds(delay);

        if (mousehover)
        {
            Debug.Log($"Executing action: {videoAction}");
            switch (videoAction)
            {
                case "PlayPause":
                    TogglePlayPause();
                    playPauseToggle.HoverIcon(); // Call HoverIcon on the playPauseToggle instance
                    break;
                case "Stop":
                    StopVideo();
                    stopToggle.SelectIcon();
                    break;
                case "FastForward":
                    StartCoroutine(ContinuousAction(FastForward));
                    fastForwardToggle.HoverIcon();
                    break;
                case "F_FastForward":
                    StartCoroutine(ContinuousAction(F_FastForward));
                    fFastForwardToggle.HoverIcon();
                    break;
                case "Rewind":
                    StartCoroutine(ContinuousAction(Rewind));
                    rewindToggle.HoverIcon();
                    break;
                default:
                    Debug.LogWarning("Unknown video action: " + videoAction);
                    break;
            }
        }
    }

    private IEnumerator ContinuousAction(System.Action action)
    {
        Debug.Log("ContinuousAction started");
        while (mousehover)
        {
            action();
            yield return null;
        }
        // Resume normal playback when the pointer is removed
        videoPlayer.playbackSpeed = 1.0f;
        Debug.Log("ContinuousAction ended: Playback speed reset to 1.0");
    }

    // mouse Exit Event
    public void MouseExit()
    {
        Debug.Log("MouseExit called");
        mousehover = false;
        counter = 0;
        StopCoroutine(HandleMouseHover());
        ResetIcons();
        // Resume normal playback
        videoPlayer.playbackSpeed = 1.0f;
    }

    private void ResetIcons()
    {
        playPauseToggle.DefaultIcon();
        stopToggle.DefaultIcon();
        fastForwardToggle.DefaultIcon();
        fFastForwardToggle.DefaultIcon();
        rewindToggle.DefaultIcon();
    }
}
