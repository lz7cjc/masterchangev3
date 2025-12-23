using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple visual countdown for gaze interactions
/// Shows a ring that fills up while hovering over interactive objects
/// Optional - works without this component too
/// </summary>
public class GazeHUDCountdown : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Image countdownRing;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Color activeColor = new Color(0, 1, 1, 0.7f);
    [SerializeField] private Color completeColor = new Color(0, 1, 0, 1f);

    [Header("Auto-Create")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private float ringSize = 100f;

    private CanvasGroup canvasGroup;
    private float targetAlpha = 0f;
    private float currentProgress = 0f;
    private bool isActive = false;

    void Awake()
    {
        if (autoCreateUI && countdownRing == null)
        {
            CreateCountdownUI();
        }

        // Get or add canvas group for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start hidden
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        // Smooth fade in/out
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    /// <summary>
    /// Start countdown with given duration
    /// </summary>
    public void StartCountdown(float duration)
    {
        isActive = true;
        targetAlpha = 1f;
        currentProgress = 0f;

        if (countdownRing != null)
        {
            countdownRing.fillAmount = 0f;
            countdownRing.color = activeColor;
        }
    }

    /// <summary>
    /// Update countdown progress (0-1)
    /// </summary>
    public void UpdateCountdown(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (countdownRing != null)
        {
            countdownRing.fillAmount = currentProgress;

            // Change color when complete
            if (currentProgress >= 1f)
            {
                countdownRing.color = completeColor;
            }
        }
    }

    /// <summary>
    /// Reset and hide countdown
    /// </summary>
    public void ResetCountdown()
    {
        isActive = false;
        targetAlpha = 0f;
        currentProgress = 0f;

        if (countdownRing != null)
        {
            countdownRing.fillAmount = 0f;
        }
    }

    /// <summary>
    /// Hide countdown immediately
    /// </summary>
    public void HideCountdown()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        targetAlpha = 0f;
    }

    private void CreateCountdownUI()
    {
        // Create canvas if we're not already on one
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GazeCountdownCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            transform.SetParent(canvasObj.transform);
        }

        // Create countdown ring image
        GameObject ringObj = new GameObject("CountdownRing");
        ringObj.transform.SetParent(transform);

        countdownRing = ringObj.AddComponent<Image>();
        countdownRing.type = Image.Type.Filled;
        countdownRing.fillMethod = Image.FillMethod.Radial360;
        countdownRing.fillOrigin = (int)Image.Origin360.Top;
        countdownRing.fillClockwise = true;
        countdownRing.fillAmount = 0f;
        countdownRing.color = activeColor;

        // Position in center of screen
        RectTransform rectTransform = ringObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(ringSize, ringSize);
        rectTransform.anchoredPosition = Vector2.zero;

        // Create sprite for ring
        countdownRing.sprite = CreateRingSprite();

        Debug.Log("[GazeHUDCountdown] Auto-created countdown UI");
    }

    private Sprite CreateRingSprite()
    {
        // Create a simple ring texture
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f;
        float innerRadius = outerRadius * 0.7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                if (dist <= outerRadius && dist >= innerRadius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}