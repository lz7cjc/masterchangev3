using UnityEngine;

public class HUDRotationCompensator : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Counter-rotate the HUD to negate the camera's rotation
        transform.rotation = Quaternion.Inverse(mainCamera.transform.rotation);
    }
}