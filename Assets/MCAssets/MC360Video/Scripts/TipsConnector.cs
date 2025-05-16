using UnityEngine;

// This script connects the video controller with the tips panel
public class TipsConnector : MonoBehaviour
{
    public VRTipsPanel tipsPanel;
    public GameObject tipsContainer; // The parent object containing all tip buttons

    // Reference to our gaze-based interaction system 
    public Transform reticle;
    public float gazeTime = 2.0f; // How long the user needs to look at a button

    private bool isTipping = false;
    private Transform currentGazedObject = null;
    private float gazeTimer = 0f;

    // Called by showfilm.tipping()
    public void ActivateTipping()
    {
        if (tipsPanel != null)
        {
            tipsPanel.ShowTipsPanel();
            isTipping = true;
        }
        else
        {
            Debug.LogError("Tips panel reference not set!");
        }
    }

    void Update()
    {
        if (!isTipping)
            return;

        // Handle reticle-based interaction with the tip buttons
        HandleGazeInteraction();
    }

    void HandleGazeInteraction()
    {
        // Cast a ray from the camera through the reticle
        Ray ray = new Ray(Camera.main.transform.position, reticle.position - Camera.main.transform.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Check if we're looking at a tip button
            TipButton tipButton = hit.collider.GetComponent<TipButton>();

            if (tipButton != null)
            {
                // If looking at a new object, reset timer
                if (currentGazedObject != hit.transform)
                {
                    currentGazedObject = hit.transform;
                    gazeTimer = 0f;

                    // Visual feedback that button is being gazed at
                    tipButton.OnGazeEnter();
                }

                // Increment gaze timer
                gazeTimer += Time.deltaTime;

                // If gazed long enough, activate the button
                if (gazeTimer >= gazeTime)
                {
                    tipButton.OnSelect();
                    gazeTimer = 0f;
                }
            }
            else
            {
                // Not looking at a tip button
                ResetGaze();
            }
        }
        else
        {
            // Not looking at anything
            ResetGaze();
        }
    }

    void ResetGaze()
    {
        if (currentGazedObject != null)
        {
            TipButton tipButton = currentGazedObject.GetComponent<TipButton>();
            if (tipButton != null)
            {
                tipButton.OnGazeExit();
            }

            currentGazedObject = null;
            gazeTimer = 0f;
        }
    }

    // Call this after a tip has been selected
    public void TipSelected(int amount)
    {
        Debug.Log("Tip selected: R$" + amount);

        // Handle the tip selection (e.g., save to player prefs, update balance, etc.)
        PlayerPrefs.SetInt("LastTipAmount", amount);

        // Close the tipping panel
        CloseTippingPanel();
    }

    public void CloseTippingPanel()
    {
        if (tipsPanel != null)
        {
            tipsPanel.HideTipsPanel();
            isTipping = false;
        }
    }
}