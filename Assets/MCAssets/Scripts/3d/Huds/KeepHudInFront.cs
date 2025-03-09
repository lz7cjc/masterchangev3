using UnityEngine;

public class KeepHUDInFront : MonoBehaviour
{
    public Transform cameraTransform; // Reference to the camera

    void Update()
    {
        // Keep the HUD in front of the camera
        transform.position = cameraTransform.position + cameraTransform.forward * 2f; // Adjust the distance as needed
        transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
    }
}
