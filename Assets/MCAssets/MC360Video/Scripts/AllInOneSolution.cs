using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimplifiedVideoHandler : MonoBehaviour
{
    [SerializeField] private string description;
    [SerializeField] private string prefabType = "Default";
    [SerializeField] private string zoneName;

    [Header("Selection Settings")]
    [SerializeField] private float selectionTimeThreshold = 2.0f;
    [SerializeField] private string playerPrefsKey = "VideoUrl";
    [SerializeField] private string videoAppScene = "360VideoApp";

    [Header("Scene Management")]
    [SerializeField] private string returntoscene;
    [SerializeField] private string nextscene = "360VideoApp";
    [SerializeField] private int returnstage = 0;
    [SerializeField] private string behaviour = "Fun";
    [SerializeField] private bool useAdditiveLoading = true;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.green;
    [SerializeField] private bool useProgressIndicator = true;
    [SerializeField] private bool rotateOnHover = true;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private bool isGazing = false;
    private float gazeTimer = 0.0f;
    private bool isSelected = false;
    private Quaternion originalRotation;
    private RectTransform progressBar;
    private Canvas progressCanvas;
    private Image progressBarImage;
    private bool isRotating = false;
    private BoxCollider boxCollider;
    private EventTrigger eventTrigger;
    private bool initialized = false;
    private Vector3 initialPosition;
    private bool isDragging = false;
    private string videoUrl;
    private string title;

    private void Awake()
    {
        originalRotation = transform.rotation;
        initialPosition = transform.position;

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.size = new Vector3(2, 2, 0.2f);
        boxCollider.isTrigger = true;

        SetupEventTrigger();

        if (useProgressIndicator)
            CreateProgressBar();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(returntoscene) && !string.IsNullOrEmpty(zoneName))
            returntoscene = zoneName;

        if (titleText == null)
            titleText = GetComponentInChildren<TextMeshProUGUI>();

        if ((titleText == null || descriptionText == null) && (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(description)))
        {
            TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in textComponents)
            {
                if (t.name.ToLower().Contains("title")) titleText = t;
                else if (t.name.ToLower().Contains("desc")) descriptionText = t;
            }

            if (titleText == null && textComponents.Length > 0) titleText = textComponents[0];
            if (descriptionText == null && textComponents.Length > 1) descriptionText = textComponents[1];
        }

        UpdateUI();
        initialized = true;

        if (debugMode)
            Debug.Log($"Initialized video handler for: {title} ({videoUrl}), Zone: {zoneName}");
    }

    private void Update()
    {
        if (isGazing && !isSelected)
        {
            gazeTimer += Time.deltaTime;

            if (progressBar != null && progressBar.gameObject.activeSelf)
            {
                float progress = Mathf.Clamp01(gazeTimer / selectionTimeThreshold);
                progressBar.sizeDelta = new Vector2(100 * progress, progressBar.sizeDelta.y);
                if (progressBarImage != null)
                    progressBarImage.color = Color.Lerp(Color.yellow, Color.green, progress);
            }

            if (rotateOnHover && isRotating)
                transform.Rotate(Vector3.up, 30f * Time.deltaTime);

            if (gazeTimer >= selectionTimeThreshold)
                SelectVideo();
        }

#if UNITY_EDITOR
        if (isDragging && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Default", "Terrain")))
            {
                Vector3 newPos = hit.point;
                newPos.y = transform.position.y;
                transform.position = newPos;
                NotifyVideoMoved();
            }
        }
        else if (isDragging)
        {
            isDragging = false;
        }
#endif
    }

    private void SetupEventTrigger()
    {
        eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
        eventTrigger.triggers = new List<EventTrigger.Entry>();

        AddTrigger(EventTriggerType.PointerEnter, (data) => OnGazeEnter());
        AddTrigger(EventTriggerType.PointerExit, (data) => OnGazeExit());
        AddTrigger(EventTriggerType.PointerClick, (data) => SelectVideo());

#if UNITY_EDITOR
        AddTrigger(EventTriggerType.BeginDrag, (data) => {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                isDragging = true;
                initialPosition = transform.position;
            }
        });

        AddTrigger(EventTriggerType.EndDrag, (data) => {
            isDragging = false;
            if (transform.position != initialPosition)
                NotifyVideoMoved();
        });
#endif
    }

    private void AddTrigger(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener(action);
        eventTrigger.triggers.Add(entry);
    }

    public void OnGazeEnter()
    {
        isGazing = true;
        gazeTimer = 0;

        if (titleText != null) titleText.color = hoverColor;
        if (useProgressIndicator && progressBar != null) ShowProgressBar();
        if (rotateOnHover) isRotating = true;

        if (debugMode) Debug.Log($"Gaze entered: {title}");
    }

    public void OnGazeExit()
    {
        isGazing = false;
        gazeTimer = 0;

        if (titleText != null) titleText.color = normalColor;
        if (useProgressIndicator && progressBar != null) HideProgressBar();
        if (rotateOnHover)
        {
            isRotating = false;
            transform.rotation = originalRotation;
        }

        if (debugMode) Debug.Log($"Gaze exited: {title}");
    }

    public void SelectVideo()
    {
        if (isSelected || string.IsNullOrEmpty(videoUrl)) return;

        isSelected = true;

        PlayerPrefs.SetString(playerPrefsKey, videoUrl);
        PlayerPrefs.SetString("VideoUrl", videoUrl);
        PlayerPrefs.SetString("returntoscene", returntoscene);
        PlayerPrefs.SetString("behaviour", behaviour);
        PlayerPrefs.SetInt("stage", returnstage);
        PlayerPrefs.SetString("nextscene", nextscene);

        if (!string.IsNullOrEmpty(title)) PlayerPrefs.SetString("videoTitle", title);
        if (!string.IsNullOrEmpty(description)) PlayerPrefs.SetString("videoDescription", description);

        if (useAdditiveLoading)
        {
            PlayerPrefs.SetInt("comingFromMainVR", 1);
            PlayerPrefs.SetString("mainVRSceneName", SceneManager.GetActiveScene().name);
            StoreActiveGameObjects();
            SceneManager.LoadScene(videoAppScene, LoadSceneMode.Additive);
            StartCoroutine(SetSceneActive(videoAppScene));
        }
        else
        {
            SceneManager.LoadScene(videoAppScene, LoadSceneMode.Single);
        }
    }

    private void StoreActiveGameObjects()
    {
        List<string> activeObjects = new List<string>();
        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (obj.activeSelf)
            {
                activeObjects.Add(obj.name);
                obj.SetActive(false);
                if (debugMode) Debug.Log($"Deactivated GameObject: {obj.name}");
            }
        }
        PlayerPrefs.SetString("mainvr_reactivate", string.Join("|", activeObjects));
    }

    private IEnumerator SetSceneActive(string sceneName)
    {
        yield return new WaitUntil(() => {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).name == sceneName) return true;
            return false;
        });

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                SceneManager.SetActiveScene(scene);
                if (debugMode) Debug.Log($"Set {sceneName} as active scene");
                break;
            }
        }
    }

    private void CreateProgressBar()
    {
        progressCanvas = GetComponentInChildren<Canvas>();
        if (progressCanvas == null)
        {
            GameObject canvasObj = new GameObject("ProgressCanvas");
            canvasObj.transform.SetParent(transform);
            progressCanvas = canvasObj.AddComponent<Canvas>();
            progressCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObj.AddComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 100);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasRect.localPosition = new Vector3(0, 1.5f, 0);
            canvasObj.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 100;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject progressObj = new GameObject("ProgressBar");
        progressObj.transform.SetParent(progressCanvas.transform);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(progressObj.transform);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(100, 10);
        bgRect.anchoredPosition = new Vector2(0, -20);
        bgObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform);
        progressBar = fillObj.AddComponent<RectTransform>();
        progressBar.sizeDelta = new Vector2(0, 0);
        progressBarImage = fillObj.AddComponent<Image>();
        progressBarImage.color = Color.yellow;

        progressObj.SetActive(false);
        progressBar.gameObject.SetActive(false);
    }

    private void ShowProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.parent.gameObject.SetActive(true);
            progressBar.sizeDelta = new Vector2(0, 0);
        }
    }

    private void HideProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.parent.gameObject.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (titleText != null && !string.IsNullOrEmpty(title))
        {
            titleText.text = title;
            titleText.color = normalColor;
        }

        if (descriptionText != null && !string.IsNullOrEmpty(description))
            descriptionText.text = description;
    }

    private void NotifyVideoMoved()
    {
        if (!initialized) return;

        foreach (var manager in FindObjectsOfType<MonoBehaviour>())
        {
            var method = manager.GetType().GetMethod("NotifyVideoMoved");
            if (method != null && method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(GameObject))
            {
                method.Invoke(manager, new object[] { gameObject });
                if (debugMode) Debug.Log($"Notified {manager.GetType().Name} about video position change: {title}");
                return;
            }
        }

        if (debugMode)
            Debug.LogWarning("No placement manager found to notify.");
    }

    public void SetDataFromVideoEntry(VideoEntry entry, string zoneOverride = null)
    {
        if (entry == null) return;
        videoUrl = entry.PublicUrl;
        title = string.IsNullOrEmpty(entry.Title) ? entry.FileName : entry.Title;
        description = entry.Description;
        prefabType = string.IsNullOrEmpty(entry.Prefab) ? "Default" : entry.Prefab;
        zoneName = zoneOverride ?? entry.Zone;
        returntoscene = zoneName;
        UpdateUI();

        if (debugMode)
            Debug.Log($"Set data from video entry: {title} in zone {zoneName}");
    }

    public string GetVideoUrl() => videoUrl;
    public string GetZoneName() => zoneName;
}

// Other helper classes like EnhancedVideoPlayer, ToggleShowHideVideo, etc.
// can be re-added below this class — let me know if you want them included too.
