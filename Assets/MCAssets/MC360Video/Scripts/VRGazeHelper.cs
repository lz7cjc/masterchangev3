using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class VRGazeHelper : MonoBehaviour
{
    [Header("Gaze Settings")]
    public float gazeTime = 2.0f;
    public bool useGazeProgress = true;

    [Header("UI Components")]
    public Image gazeProgressImage;
    public GameObject gazeIndicator;

    [Header("Events")]
    public UnityEvent OnGazeComplete;

    private float currentGazeTime = 0f;
    private bool isGazing = false;
    private GameObject currentTarget;

    private void Start()
    {
        // Hide gaze indicator at start
        if (gazeIndicator != null)
            gazeIndicator.SetActive(false);

        // Initialize the UnityEvent if not already
        if (OnGazeComplete == null)
            OnGazeComplete = new UnityEvent();
    }

    private void Update()
    {
        if (isGazing)
        {
            currentGazeTime += Time.deltaTime;

            // Update progress indicator
            if (useGazeProgress && gazeProgressImage != null)
            {
                gazeProgressImage.fillAmount = currentGazeTime / gazeTime;
            }

            // Check if gaze is complete
            if (currentGazeTime >= gazeTime)
            {
                // Trigger the gaze complete event
                OnGazeComplete.Invoke();
                ResetGaze();
            }
        }
    }

    // Call this when the reticle enters a button
    public void StartGaze(GameObject target)
    {
        if (target == null)
            return;

        currentTarget = target;
        isGazing = true;
        currentGazeTime = 0f;

        // Show and position the gaze indicator
        if (gazeIndicator != null)
        {
            gazeIndicator.SetActive(true);

            // Position near the target if possible
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                gazeIndicator.transform.position = targetRect.position;
            }
        }

        // Reset progress indicator
        if (useGazeProgress && gazeProgressImage != null)
        {
            gazeProgressImage.fillAmount = 0f;
        }
    }

    // Call this when the reticle exits a button
    public void ResetGaze()
    {
        isGazing = false;
        currentGazeTime = 0f;
        currentTarget = null;

        // Hide the gaze indicator
        if (gazeIndicator != null)
        {
            gazeIndicator.SetActive(false);
        }

        // Reset progress indicator
        if (useGazeProgress && gazeProgressImage != null)
        {
            gazeProgressImage.fillAmount = 0f;
        }
    }

    // Helper method to get the current target
    public GameObject GetCurrentTarget()
    {
        return currentTarget;
    }
}