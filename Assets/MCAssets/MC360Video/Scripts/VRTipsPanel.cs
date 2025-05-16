using UnityEngine;

public class VRTipsPanel : MonoBehaviour
{
    public Transform mainCamera;
    public float distanceFromCamera = 2.0f;
    public bool faceCamera = true;
    public float smoothTime = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private bool isActive = false;

    void Start()
    {
        // Get camera reference if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Initially hide the panel
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive || mainCamera == null)
            return;

        // Position the panel in front of the camera at the specified distance
        Vector3 targetPosition = mainCamera.position + (mainCamera.forward * distanceFromCamera);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Make the panel face the camera
        if (faceCamera)
        {
            transform.LookAt(2 * transform.position - mainCamera.position);
        }
    }

    // Call this method to activate the tips panel when the video ends
    public void ShowTipsPanel()
    {
        gameObject.SetActive(true);
        isActive = true;

        // Position it immediately in front of the camera to start
        transform.position = mainCamera.position + (mainCamera.forward * distanceFromCamera);

        if (faceCamera)
        {
            transform.LookAt(2 * transform.position - mainCamera.position);
        }
    }

    // Call this to hide the panel
    public void HideTipsPanel()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}