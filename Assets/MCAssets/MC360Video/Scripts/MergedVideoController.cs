using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections;

public class MergedVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoPlayer audioPlayer;
    public bool mousehover = false;
    public float counter = 0;
    private string videoAction;
    public float delay = 3.0f; // Delay in seconds

    private int totalfilmlength;

    // UI Elements
    public GameObject Panel;
    public GameObject launchHuds;
    public GameObject huds;
    public GameObject loadingAssets;

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

    public Sprite spriteRewindTo0;
    public Sprite spriteHoverRewindTo0;

    public Sprite spriteRewind30s;
    public Sprite spriteHoverRewind30s;

    public GameObject playGameObject;
    public GameObject pauseGameObject;

    [Header("TMP Text Fields")]
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text filmLengthText;
    [SerializeField] private TMP_Text videoTime;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Coroutine continuousActionCoroutine;
    private bool actionTriggered = false;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Prepare();
        }

        if (showDebugInfo)
            Debug.Log("MergedVideoController: Started and prepared video player");
    }

    void Update()
    {
        if (videoPlayer == null)
            return;

        // Show/hide loading assets based on video preparation
        if (videoPlayer.isPrepared)
        {
            if (loadingAssets != null)
                loadingAssets.SetActive(false);

            totalfilmlength = System.Convert.ToInt32(videoPlayer.length);

            // Format for combined time display
            if (videoTime != null)
            {
                var tFFormat = TimeSpan.FromSeconds(totalfilmlength);
                var vTFormat = TimeSpan.FromSeconds(videoPlayer.time);
                videoTime.text = string.Format("{0:00}:{1:00}", vTFormat.TotalMinutes, vTFormat.Seconds) +
                               "/" + string.Format("{0:00}:{1:00}", tFFormat.TotalMinutes, tFFormat.Seconds);
            }

            // Activate UI elements
            if (launchHuds != null)
                launchHuds.SetActive(true);
            if (huds != null)
                huds.SetActive(true);
        }
        else
        {
            if (videoTime != null)
                videoTime.text = "Getting video info";
            if (loadingAssets != null)
                loadingAssets.SetActive(true);
        }

        // Update the elapsed time text
        if (videoPlayer.isPlaying && elapsedTimeText != null)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(videoPlayer.time);
            elapsedTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", elapsedTime.Hours, elapsedTime.Minutes, elapsedTime.Seconds);
        }

        // Handle mouse hover interactions
        if (mousehover)
        {
            counter += Time.deltaTime;

            if (showDebugInfo)
                Debug.Log("controls: Counter value is " + counter);

            if (counter >= delay && !actionTriggered)
            {
                if (showDebugInfo)
                    Debug.Log("controls: Counter exceeded delay");

                actionTriggered = true;

                if (videoPlayer.canSetPlaybackSpeed)
                {
                    switch (videoAction)
                    {
                        case "FastForward":
                            if (showDebugInfo)
                                Debug.Log("controls: FastForward action triggered");

                            if (spriterendererFF != null)
                                spriterendererFF.sprite = spriteSelectedFF;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 2f;
                                if (audioPlayer != null)
                                    audioPlayer.playbackSpeed = 2f;

                                if (showDebugInfo)
                                    Debug.Log("controls: Playback speed set to 2x");
                            }));
                            break;
                        case "FastFastForward":
                            if (showDebugInfo)
                                Debug.Log("controls: FastFastForward action triggered");

                            if (spriterendererFFFF != null)
                                spriterendererFFFF.sprite = spriteSelectedFFFF;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = 3f;
                                if (audioPlayer != null)
                                    audioPlayer.playbackSpeed = 3f;

                                if (showDebugInfo)
                                    Debug.Log("controls: Playback speed set to 3x");
                            }));
                            break;
                        case "Rewind":
                            if (showDebugInfo)
                                Debug.Log("controls: Rewind action triggered");

                            if (spriterendererFF != null)
                                spriterendererFF.sprite = spriteSelectedRewind;
                            continuousActionCoroutine = StartCoroutine(ContinuousAction(() =>
                            {
                                videoPlayer.playbackSpeed = -2f;
                                if (audioPlayer != null)
                                    audioPlayer.playbackSpeed = -2f;

                                if (showDebugInfo)
                                    Debug.Log("controls: Playback speed set to -2x");
                            }));
                            break;
                        case "Pause":
                            if (showDebugInfo)
                                Debug.Log("controls: Pause action triggered");

                            videoPlayer.Pause();
                            if (playGameObject != null) playGameObject.SetActive(true);
                            if (pauseGameObject != null) pauseGameObject.SetActive(false);
                            break;
                        case "Play":
                            if (showDebugInfo)
                                Debug.Log("controls: Play action triggered");

                            videoPlayer.Play();
                            if (playGameObject != null) playGameObject.SetActive(false);
                            if (pauseGameObject != null) pauseGameObject.SetActive(true);
                            break;
                        case "Stop":
                            if (showDebugInfo)
                                Debug.Log("controls: Stop action triggered");

                            stopDirect();
                            break;
                        default:
                            if (showDebugInfo)
                                Debug.Log("controls: No valid action specified");
                            break;
                    }

                    // Show panel if available
                    if (Panel != null)
                    {
                        Panel.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp == null)
            return;

        vp.Play();
        if (playGameObject != null) playGameObject.SetActive(false);
        if (pauseGameObject != null) pauseGameObject.SetActive(true);

        TimeSpan filmLength = TimeSpan.FromSeconds(vp.length);
        if (filmLengthText != null)
            filmLengthText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", filmLength.Hours, filmLength.Minutes, filmLength.Seconds);

        if (showDebugInfo)
            Debug.Log("controls: OnVideoPrepared: Video length set and playback started");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (showDebugInfo)
            Debug.Log("controls: Video finished playing");

        // Transition to the "tips" scene
        SceneManager.LoadScene("tips", LoadSceneMode.Single);
    }

    public void stopDirect()
    {
        if (showDebugInfo)
            Debug.Log("in stopDirect");

        // Transition to the "tips" scene
        SceneManager.LoadScene("tips", LoadSceneMode.Single);
    }

    private IEnumerator ContinuousAction(Action action)
    {
        if (showDebugInfo)
            Debug.Log("controls: ContinuousAction coroutine started");

        while (mousehover)
        {
            action();
            yield return null;
        }

        if (videoPlayer != null)
            videoPlayer.playbackSpeed = 1.0f;

        if (audioPlayer != null)
            audioPlayer.playbackSpeed = 1.0f;

        if (showDebugInfo)
            Debug.Log("controls: Playback speed reset to 1x");

        ResetIcons();

        if (showDebugInfo)
            Debug.Log("controls: ContinuousAction coroutine ended");

        continuousActionCoroutine = null;
    }

    public void MouseEnter(string action)
    {
        mousehover = true;
        counter = 0f;
        actionTriggered = false;
        videoAction = action;

        if (showDebugInfo)
            Debug.Log($"controls: MouseEnter called with action '{action}', counter reset");
    }


    public void MouseExit()
    {
        mousehover = false;
        counter = 0;
        actionTriggered = false;

        if (showDebugInfo)
            Debug.Log("controls: MouseExit called, counter reset");

        if (continuousActionCoroutine != null)
        {
            StopCoroutine(continuousActionCoroutine);
            continuousActionCoroutine = null;
        }
        ResetIcons();

        if (videoPlayer != null)
            videoPlayer.playbackSpeed = 1.0f;

        if (audioPlayer != null)
            audioPlayer.playbackSpeed = 1.0f;
    }

    private void ResetIcons()
    {
        if (spriterendererPlay != null) spriterendererPlay.sprite = spriteDefaultPlay;
        if (spriterendererPause != null) spriterendererPause.sprite = spriteDefaultPause;
        if (spriterendererStop != null) spriterendererStop.sprite = spriteDefaultStop;
        if (spriterendererFF != null) spriterendererFF.sprite = spriteDefaultFF;
        if (spriterendererFFFF != null) spriterendererFFFF.sprite = spriteDefaultFFFF;

        if (showDebugInfo)
            Debug.Log("controls: Icons reset to default state");
    }
}
