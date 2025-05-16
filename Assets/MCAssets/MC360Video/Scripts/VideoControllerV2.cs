using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;

public class VideoControllerV2 : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoPlayer audioPlayer;
    public bool mousehover = false;
    public float counter = 0;
    private string videoAction;
    public float delay = 3.0f; // Delay in seconds

    private bool pause;
    private bool play;
    private bool ff;
    private bool stop;
    private bool ffff;

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

    public GameObject playGameObject;
    public GameObject pauseGameObject;

    public GameObject[] objectsToShowOnStop;
    public GameObject[] objectsToHideOnStop;

    [Header("TMP Text Fields")]
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text filmLengthText;

    private Coroutine continuousActionCoroutine;
    private bool actionTriggered = false;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Prepare();

        Debug.Log("VideoController Start: VideoPlayer prepared");
    }

    void Update()
    {
        // Update the elapsed time text
        if (videoPlayer.isPlaying)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(videoPlayer.time);
            elapsedTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", elapsedTime.Hours, elapsedTime.Minutes, elapsedTime.Seconds);
        }

        if (mousehover)
        {
            counter += Time.deltaTime;
            Debug.Log("controls: Counter value is " + counter);
            if (counter >= delay && !actionTriggered)
            {
                Debug.Log("controls: Counter exceeded delay");
                actionTriggered = true;

                if (videoPlayer.canSetPlaybackSpeed)
                {
                    switch (videoAction)
                    {
                        case "FastForward":
                            Debug.Log("controls: FastForward action triggered");
                            spriterendererFF.sprite = spriteSelectedFF;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 2f;
                                audioPlayer.playbackSpeed = 2f;
                                Debug.Log("controls: Playback speed set to 2x" + videoPlayer.playbackSpeed);
                            }));
                            break;
                        case "FastFastForward":
                            Debug.Log("controls: FastFastForward action triggered");
                            spriterendererFFFF.sprite = spriteSelectedFFFF;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 3f;
                                audioPlayer.playbackSpeed = 3f;
                                Debug.Log("controls: Playback speed set to 3x" + videoPlayer.playbackSpeed);
                            }));
                            break;
                        case "Rewind":
                            Debug.Log("controls: Rewind action triggered");
                            spriterendererFF.sprite = spriteSelectedRewind;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = -2f;
                                audioPlayer.playbackSpeed = -2f;
                                Debug.Log("controls: Playback speed set to -2x" + videoPlayer.playbackSpeed);
                            }));
                            break;
                        case "Pause":
                            Debug.Log("controls: Pause action triggered");
                            videoPlayer.Pause();
                            playGameObject.SetActive(true);
                            pauseGameObject.SetActive(false);
                            break;
                        case "Play":
                            Debug.Log("controls: Play action triggered");
                            videoPlayer.Play();
                            playGameObject.SetActive(false);
                            pauseGameObject.SetActive(true);
                            break;
                        case "Stop":
                            Debug.Log("controls: Stop action triggered");
                            StopAndReset();
                            break;
                        default:
                            Debug.Log("controls: No valid action specified");
                            break;
                    }
                }
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoPlayer.Play();
        playGameObject.SetActive(false);
        pauseGameObject.SetActive(true);

        TimeSpan filmLength = TimeSpan.FromSeconds(videoPlayer.length);
        filmLengthText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", filmLength.Hours, filmLength.Minutes, filmLength.Seconds);

        Debug.Log("controls: OnVideoPrepared: Video length set and playback started");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("controls: Video finished playing");
        StopAndReset();
    }

    private void StopAndReset()
    {
        videoPlayer.Stop();
        PlayerPrefs.DeleteKey("VideoUrl");
        //added when split scene 24/12
        //    SceneManager.LoadScene("everything");
        showfilm = FindObjectOfType<showfilm>();
        showfilm.tipping();
        //SceneManager.LoadScene("videoVote");  
        stop = false;
    }

    public void MouseHover(string action)
    {
        videoAction = action;
        mousehover = true;
        counter = 0;
        actionTriggered = false;
        Debug.Log("controls: MouseHover called with action " + videoAction);

        // Change the icon to hover state immediately
        switch (action)
        {
            case "FastForward":
                spriterendererFF.sprite = spriteHoverFF;
                break;
            case "FastFastForward":
                spriterendererFFFF.sprite = spriteHoverFFFF;
                break;
            case "Rewind":
                spriterendererFF.sprite = spriteHoverRewind;
                break;
            case "Pause":
                spriterendererPause.sprite = spriteHoverPause;
                break;
            case "Play":
                spriterendererPlay.sprite = spriteHoverPlay;
                break;
            case "Stop":
                spriterendererStop.sprite = spriteHoverStop;
                break;
            default:
                Debug.Log("controls: No valid hover action specified");
                break;
        }
    }

    private IEnumerator ContinuousAction(Action action)
    {
        Debug.Log("controls: ContinuousAction coroutine started");
        while (mousehover)
        {
            action();
            yield return null;
        }
        videoPlayer.playbackSpeed = 1.0f;
        audioPlayer.playbackSpeed = 1.0f;
        Debug.Log("controls: Playback speed reset to 1x");
        ResetIcons();
        Debug.Log("controls: ContinuousAction coroutine ended");
        continuousActionCoroutine = null;
    }

    public void MouseExit()
    {
        mousehover = false;
        counter = 0;
        actionTriggered = false;
        Debug.Log("controls: MouseExit called, counter reset");
        if (continuousActionCoroutine != null)
        {
            StopCoroutine(continuousActionCoroutine);
            continuousActionCoroutine = null;
        }
        ResetIcons();
        videoPlayer.playbackSpeed = 1.0f; // Ensure playback speed is reset
        audioPlayer.playbackSpeed = 1.0f; // Ensure playback speed is reset

        // Additional logic to handle fast fast forward cancellation
        if (videoAction == "FastFastForward")
        {
            spriterendererFFFF.sprite = spriteDefaultFFFF;
            Debug.Log("controls: FastFastForward action cancelled");
        }
    }

    private void ResetIcons()
    {
        spriterendererPlay.sprite = spriteDefaultPlay;
        spriterendererPause.sprite = spriteDefaultPause;
        spriterendererStop.sprite = spriteDefaultStop;
        spriterendererFF.sprite = spriteDefaultFF; // Ensure this line sets the correct default FF sprite
        spriterendererFFFF.sprite = spriteDefaultFFFF;
        Debug.Log("controls: Icons reset to default state");
    }
}




