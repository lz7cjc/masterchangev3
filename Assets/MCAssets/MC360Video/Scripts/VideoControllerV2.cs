using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections;

public class VideoControllerV2 : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoPlayer audioPlayer;
    public bool mousehover = false;
    public float counter = 0;
    private string videoAction;
    public float delay = 3.0f; // Delay in seconds
   
    private int totalfilmlength;
    private showfilm showfilm;

    public SpriteRenderer spriterendererPlay;
    public SpriteRenderer spriterendererPause;
    public SpriteRenderer spriterendererStop;
    public SpriteRenderer spriterendererFF;
    public SpriteRenderer spriterendererFFFF;

    public Sprite spriteDefaultPause;
    public Sprite spriteHoverPause;

    public Sprite spriteDefaultStop;
    public Sprite spriteHoverStop;

    public Sprite spriteDefaultPlay;
    public Sprite spriteHoverPlay;

    public Sprite spriteDefaultFF;
    public Sprite spriteHoverFF;
    public Sprite spriteSelectedFF;

    public Sprite spriteDefaultFFFF;
    public Sprite spriteHoverFFFF;
    public Sprite spriteSelectedFFFF;

    public Sprite spriteDefaultRewind;
    public Sprite spriteHoverRewind;
    public Sprite spriteSelectedRewind;

    public GameObject[] objectsToShowOnStop;
    public GameObject[] objectsToHideOnStop;

    [Header("TMP Text Fields")]
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text filmLengthText;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();

        Debug.Log("VideoController Start: VideoPlayer prepared");
    }

    void Update()
    {
        if (videoPlayer.isPlaying)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(videoPlayer.time);
            elapsedTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", elapsedTime.Hours, elapsedTime.Minutes, elapsedTime.Seconds);
        }

        if (mousehover)
        {
            counter += Time.deltaTime;
            if (counter >= delay)
            {
                mousehover = false;
                counter = 0;

                if (videoPlayer.canSetPlaybackSpeed)
                {
                    switch (videoAction)
                    {
                        case "FastForward":
                            Debug.Log("Vid: FastForward");
                            spriterendererFF.sprite = spriteSelectedFF;
                            StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 2f;
                                audioPlayer.playbackSpeed = 2f;
                                Debug.Log("vid: Playback speed set to 2x" + videoPlayer.playbackSpeed);
                            }));
                            break;
                        case "FastFastForward":
                            Debug.Log("Vid: FastFastForward");
                            spriterendererFFFF.sprite = spriteSelectedFFFF;
                            StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 3f;
                                audioPlayer.playbackSpeed = 3f;
                                Debug.Log("vid: Playback speed set to 3x" + videoPlayer.playbackSpeed);
                            }));
                            break;
                        case "Rewind":
                            Debug.Log("Vid: Rewind");
                            spriterendererFF.sprite = spriteSelectedRewind;
                            StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.time = Math.Max(0, videoPlayer.time - 1);
                                audioPlayer.time = Math.Max(0, audioPlayer.time - 1);
                                Debug.Log("vid: Rewinding by 1 second");
                            }));
                            break;
                    }
                }
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoPlayer.Play();
        spriterendererPlay.gameObject.SetActive(false);
        spriterendererPause.gameObject.SetActive(true);

        TimeSpan filmLength = TimeSpan.FromSeconds(videoPlayer.length);
        filmLengthText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", filmLength.Hours, filmLength.Minutes, filmLength.Seconds);

        Debug.Log("OnVideoPrepared: Video length set and playback started");
    }

    public void MouseHover(string action)
    {
        videoAction = action;
        mousehover = true;
        counter = 0;
        Debug.Log("Vid: videoAction is " + videoAction);

        switch (action)
        {
            case "FastForward":
                spriterendererFF.sprite = spriteHoverFF;
                break;
            case "FastFastForward":
                spriterendererFFFF.sprite = spriteHoverFFFF;
                break;
        }
    }

    private IEnumerator ContinuousAction(Action action)
    {
        Debug.Log("vid: in ienumerator" + action);
        while (mousehover)
        {
            action();
            yield return null;
        }
        videoPlayer.playbackSpeed = 1.0f;
        audioPlayer.playbackSpeed = 1.0f;
        Debug.Log("vid: Playback speed reset to 1x");
        ResetIcons();
    }

    public void MouseExit()
    {
        mousehover = false;
        counter = 0;
        ResetIcons();
    }

    private void ResetIcons()
    {
        spriterendererPlay.sprite = spriteDefaultPlay;
        spriterendererPause.sprite = spriteDefaultPause;
        spriterendererStop.sprite = spriteDefaultStop;
        spriterendererFF.sprite = spriteDefaultFF;
        spriterendererFFFF.sprite = spriteDefaultFFFF;
        spriterendererFF.sprite = spriteDefaultRewind;
    }
}
