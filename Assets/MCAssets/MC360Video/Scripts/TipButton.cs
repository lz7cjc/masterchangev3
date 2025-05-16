using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TipButton : MonoBehaviour
{
    public int tipAmount;
    public TipsConnector tipsConnector;

    // Visual elements for feedback
    public Image buttonImage;
    public TMP_Text buttonText;
    public Color defaultColor = Color.white;
    public Color hoverColor = Color.green;
    public Color selectedColor = Color.yellow;

    // Optional animation parameters
    public float pulseAmount = 1.1f;
    public float pulseSpeed = 2.0f;

    private Vector3 originalScale;
    private bool isGazed = false;

    void Start()
    {
        originalScale = transform.localScale;

        // Find TipsConnector if not set
        if (tipsConnector == null)
        {
            tipsConnector = FindObjectOfType<TipsConnector>();
        }

        // Set initial visual state
        if (buttonImage != null)
        {
            buttonImage.color = defaultColor;
        }
    }

    void Update()
    {
        // Simple pulse animation when gazed
        if (isGazed)
        {
            float pulse = 1.0f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * (pulseAmount - 1.0f);
            transform.localScale = originalScale * pulse;
        }
    }

    // Called when gaze enters this button
    public void OnGazeEnter()
    {
        isGazed = true;
        if (buttonImage != null)
        {
            buttonImage.color = hoverColor;
        }
    }

    // Called when gaze leaves this button
    public void OnGazeExit()
    {
        isGazed = false;
        transform.localScale = originalScale;
        if (buttonImage != null)
        {
            buttonImage.color = defaultColor;
        }
    }

    // Called when the button is selected (after gazing long enough)
    public void OnSelect()
    {
        if (buttonImage != null)
        {
            buttonImage.color = selectedColor;
        }

        // Animate selection
        transform.localScale = originalScale * pulseAmount;

        // Invoke the tip action
        if (tipsConnector != null)
        {
            tipsConnector.TipSelected(tipAmount);
        }
        else
        {
            Debug.LogError("TipsConnector reference not set on TipButton!");
        }
    }
}