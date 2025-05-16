using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles video selection by tracking gaze/selection time and loading the video player scene
/// Enhanced with better visual feedback and resilient error handling
/// </summary>
public class VideoSelectionHandler : MonoBehaviour
{
    [SerializeField] private string videoUrl;
    [SerializeField] private float selectionTimeThreshold = 2.0f;
    [SerializeField] private string playerPrefsKey = "VideoUrl";
    [SerializeField] private string videoAppScene = "360VideoApp";

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private bool useHighlightOnGaze = true;
    [SerializeField] private float growScale = 1.1f; // Optional grow effect

    private bool isGazing = false;
    private float gazeTimer = 0.0f;
    private bool isSelected = false;

    // Visual feedback - optional components
    private List<Material> originalMaterials = new List<Material>();
    private Renderer[] objectRenderers;
    private Vector3 originalScale;

    // Audio feedback - optional component
    private AudioSource audioSource;

    // Progress visualization
    private GameObject progressIndicator;
    private Transform progressBar;

    private void Awake()
    {
        // Ensure event trigger exists and is properly set up
        SetupEventTrigger();

        // Store the original scale
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // Cache all renderers (for visual feedback)
        objectRenderers = GetComponentsInChildren<Renderer>();

        // Store original materials
        StoreMaterials();

        // Cache audio source if present (for audio feedback)
        audioSource = GetComponent<AudioSource>();

        // Create progress indicator
        if (useHighlightOnGaze)
        {
            CreateProgressIndicator();
        }

        // Double-check that event trigger is set up
        EventTrigger eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null || eventTrigger.triggers.Count == 0)
        {
            Debug.LogWarning("EventTrigger missing or empty on " + gameObject.name + ". Setting up again.");
            SetupEventTrigger();
        }
    }

    private void Update()
    {
        // Update gaze timer if being gazed at
        if (isGazing && !isSelected)
        {
            gazeTimer += Time.deltaTime;

            // Update visual feedback based on gaze progress
            UpdateVisualFeedback();

            // Check if selection threshold is reached
            if (gazeTimer >= selectionTimeThreshold)
            {
                SelectVideo();
            }
        }
    }

    private void OnEnable()
    {
        // Reset state when enabled
        isGazing = false;
        isSelected = false;
        gazeTimer = 0;
        ResetVisualFeedback();

        // Check if event trigger is set up when enabled
        EventTrigger eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null || eventTrigger.triggers.Count == 0)
        {
            SetupEventTrigger();
        }
    }

    /// <summary>
    /// Initialize the component with the video URL and settings
    /// </summary>
    public void Initialize(string url, float threshold, string prefsKey, string sceneName)
    {
        videoUrl = url;
        selectionTimeThreshold = threshold;
        playerPrefsKey = prefsKey;
        videoAppScene = sceneName;

        // Re-setup event trigger after initialization
        SetupEventTrigger();

        // Log initialization
        Debug.Log($"VideoSelectionHandler initialized for {gameObject.name} with URL: {url}");
    }

    /// <summary>
    /// Set up event triggers for pointer enter/exit to track gaze
    /// </summary>
    private void SetupEventTrigger()
    {
        EventTrigger eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // If triggers list is null, initialize it
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Clear existing entries to avoid duplicates
        eventTrigger.triggers.Clear();

        // Add pointer enter event (gaze starts)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback = new EventTrigger.TriggerEvent();
        enterEntry.callback.AddListener((data) => { OnGazeEnter(); });
        eventTrigger.triggers.Add(enterEntry);

        // Add pointer exit event (gaze ends)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback = new EventTrigger.TriggerEvent();
        exitEntry.callback.AddListener((data) => { OnGazeExit(); });
        eventTrigger.triggers.Add(exitEntry);

        // Add click event as an alternative activation method
        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback = new EventTrigger.TriggerEvent();
        clickEntry.callback.AddListener((data) => { SelectVideo(); });
        eventTrigger.triggers.Add(clickEntry);
    }

    /// <summary>
    /// Store all materials in children for later restoration
    /// </summary>
    private void StoreMaterials()
    {
        originalMaterials.Clear();

        if (objectRenderers != null && objectRenderers.Length > 0)
        {
            foreach (Renderer renderer in objectRenderers)
            {
                if (renderer != null && renderer.materials != null && renderer.materials.Length > 0)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat != null)
                        {
                            // Create a copy of the material to keep the original settings
                            originalMaterials.Add(new Material(mat));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Create a simple progress indicator
    /// </summary>
    private void CreateProgressIndicator()
    {
        // Check if we already have one
        if (progressIndicator != null)
            return;

        // Create root object for progress indicator
        progressIndicator = new GameObject("ProgressIndicator");
        progressIndicator.transform.SetParent(transform);
        progressIndicator.transform.localPosition = new Vector3(0, 1.0f, 0); // Position above the object
        progressIndicator.transform.localRotation = Quaternion.identity;

        // Create the progress bar background
        GameObject barBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barBg.name = "ProgressBarBg";
        barBg.transform.SetParent(progressIndicator.transform);
        barBg.transform.localPosition = Vector3.zero;
        barBg.transform.localScale = new Vector3(1.0f, 0.1f, 0.1f);

        // Set a dark material for background
        Renderer bgRenderer = barBg.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            bgRenderer.material = new Material(Shader.Find("Standard"));
            bgRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        // Create the progress bar fill
        GameObject barFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barFill.name = "ProgressBarFill";
        barFill.transform.SetParent(barBg.transform);
        barFill.transform.localPosition = new Vector3(-0.5f, 0, 0); // Start from left
        barFill.transform.localScale = new Vector3(0.01f, 0.8f, 0.8f); // Start tiny

        // Set the progress color
        Renderer fillRenderer = barFill.GetComponent<Renderer>();
        if (fillRenderer != null)
        {
            fillRenderer.material = new Material(Shader.Find("Standard"));
            fillRenderer.material.color = progressColor;
        }

        // Store reference to the fill for updating
        progressBar = barFill.transform;

        // Hide initially
        progressIndicator.SetActive(false);
    }

    /// <summary>
    /// Called when gaze enters this object
    /// </summary>
    public void OnGazeEnter()
    {
        isGazing = true;
        gazeTimer = 0f;

        // Show progress indicator
        if (progressIndicator != null)
        {
            progressIndicator.SetActive(true);
        }

        // Apply highlight effect if we have renderers
        if (useHighlightOnGaze && objectRenderers != null)
        {
            foreach (Renderer renderer in objectRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        // Add emission for highlighting
                        renderer.materials[i].EnableKeyword("_EMISSION");
                        renderer.materials[i].SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f));
                    }
                }
            }

            // Slightly scale up the object for feedback
            transform.localScale = originalScale * growScale;
        }

        // Play audio cue if available
        if (audioSource != null && !isSelected)
        {
            audioSource.Play();
        }
    }

    /// <summary>
    /// Called when gaze exits this object
    /// </summary>
    public void OnGazeExit()
    {
        isGazing = false;
        gazeTimer = 0f;

        // Hide progress indicator
        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        // Reset visual feedback
        ResetVisualFeedback();

        // Stop audio if playing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Called when selection threshold is reached or on click
    /// </summary>
    public void SelectVideo()
    {
        if (isSelected) return; // Prevent multiple selections

        isSelected = true;

        // Store URL in PlayerPrefs for the video player scene to access
        if (!string.IsNullOrEmpty(videoUrl))
        {
            // Store using our key
            PlayerPrefs.SetString(playerPrefsKey, videoUrl);

            // Legacy support - also store using the key expected by the 360VideoApp scene
            PlayerPrefs.SetString("VideoUrl", videoUrl);

            // Also store "VideoURL" variant (common typo/inconsistency)
            PlayerPrefs.SetString("VideoURL", videoUrl);

            // Save PlayerPrefs
            PlayerPrefs.Save();

            Debug.Log($"Video selected: {videoUrl}. URL stored in PlayerPrefs.");

            // Load the video player scene
            if (!string.IsNullOrEmpty(videoAppScene))
            {
                // Give a brief delay before loading the scene
                StartCoroutine(LoadSceneWithDelay(0.5f));
            }
        }
        else
        {
            Debug.LogError("Cannot select video: URL is empty for " + gameObject.name);
            isSelected = false;
        }
    }

    /// <summary>
    /// Load the video player scene after a delay
    /// </summary>
    private IEnumerator LoadSceneWithDelay(float delay)
    {
        // Final visual feedback
        ApplySelectedVisualState();

        yield return new WaitForSeconds(delay);

        // First try using the configured scene name
        bool sceneExists = DoesSceneExist(videoAppScene);

        // If not found, try the legacy "360VideoApp" scene name
        if (!sceneExists && videoAppScene != "360VideoApp")
        {
            if (DoesSceneExist("360VideoApp"))
            {
                Debug.Log("Using legacy '360VideoApp' scene name instead of configured scene name");
                videoAppScene = "360VideoApp";
                sceneExists = true;
            }
        }

        // Also try some other common scene names
        if (!sceneExists)
        {
            string[] commonSceneNames = { "VideoApp", "VideoPlayer", "360Player" };
            foreach (string sceneName in commonSceneNames)
            {
                if (DoesSceneExist(sceneName))
                {
                    Debug.Log($"Using alternative scene name '{sceneName}' instead of configured scene name");
                    videoAppScene = sceneName;
                    sceneExists = true;
                    break;
                }
            }
        }

        if (sceneExists)
        {
            Debug.Log($"Loading scene: {videoAppScene}");
            SceneManager.LoadScene(videoAppScene);
        }
        else
        {
            Debug.LogError($"Scene '{videoAppScene}' not found in build settings. Make sure to add it to your build settings.");
            isSelected = false; // Reset selection state so user can try again
            ResetVisualFeedback();
        }
    }

    /// <summary>
    /// Check if a scene exists in build settings
    /// </summary>
    private bool DoesSceneExist(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Update visual feedback based on gaze progress
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (isSelected) return;

        // Update progress bar scale
        if (progressBar != null)
        {
            float progress = Mathf.Clamp01(gazeTimer / selectionTimeThreshold);
            progressBar.localScale = new Vector3(progress, 0.8f, 0.8f);
            progressBar.localPosition = new Vector3(-0.5f + (progress * 0.5f), 0, 0); // Grow from left to right

            // Update color from yellow to green based on progress
            Renderer fillRenderer = progressBar.GetComponent<Renderer>();
            if (fillRenderer != null)
            {
                fillRenderer.material.color = Color.Lerp(Color.yellow, progressColor, progress);
            }
        }

        // Update renderer colors if we have them
        if (objectRenderers != null && objectRenderers.Length > 0)
        {
            float progress = Mathf.Clamp01(gazeTimer / selectionTimeThreshold);

            foreach (Renderer renderer in objectRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        // Increase emission based on progress
                        Color emissionColor = Color.Lerp(new Color(0.2f, 0.2f, 0.2f), progressColor, progress);
                        renderer.materials[i].SetColor("_EmissionColor", emissionColor);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Apply the "selected" visual state
    /// </summary>
    private void ApplySelectedVisualState()
    {
        // Hide progress bar
        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        // Apply selected visual state to renderers
        if (objectRenderers != null)
        {
            foreach (Renderer renderer in objectRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        // Set a bright emission
                        renderer.materials[i].EnableKeyword("_EMISSION");
                        renderer.materials[i].SetColor("_EmissionColor", selectedColor);

                        // Also change base color
                        if (renderer.materials[i].HasProperty("_Color"))
                        {
                            renderer.materials[i].color = selectedColor;
                        }
                    }
                }
            }
        }

        // Add a little "pop" animation
        StartCoroutine(PopAnimation());
    }

    /// <summary>
    /// A little pop animation when selected
    /// </summary>
    private IEnumerator PopAnimation()
    {
        float duration = 0.2f;
        float timer = 0f;
        float maxScale = 1.3f;

        // First grow
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(1.0f, maxScale, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // Then shrink
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(maxScale, 1.0f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // Reset to original
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Reset visual feedback when gaze exits
    /// </summary>
    private void ResetVisualFeedback()
    {
        // Reset scale
        transform.localScale = originalScale;

        // Reset renderers to original materials
        if (objectRenderers != null && originalMaterials.Count > 0)
        {
            int materialIndex = 0;

            foreach (Renderer renderer in objectRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        if (materialIndex < originalMaterials.Count)
                        {
                            // Copy key properties from original materials
                            if (renderer.materials[i].HasProperty("_Color") && originalMaterials[materialIndex].HasProperty("_Color"))
                            {
                                renderer.materials[i].color = originalMaterials[materialIndex].color;
                            }

                            // Reset emission
                            renderer.materials[i].DisableKeyword("_EMISSION");

                            materialIndex++;
                        }
                    }
                }
            }
        }

        // Hide progress indicator
        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }
    }
}