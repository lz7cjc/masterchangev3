using UnityEngine;

/// <summary>
/// Makes this GameObject always face the camera
/// Attach to any UI element in a 360° environment that
/// should always face the user
/// </summary>
public class Billboard : MonoBehaviour
{
    public Transform cameraTransform;
    public bool flipHorizontally = false;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        // Make the object face the camera
        transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                         cameraTransform.rotation * Vector3.up);

        // Optional horizontal flip (if text appears backwards)
        if (flipHorizontally)
        {
            transform.Rotate(0, 180, 0);
        }
    }
}