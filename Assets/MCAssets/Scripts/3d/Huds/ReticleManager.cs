using UnityEngine;

public class ReticleManager : MonoBehaviour
{
    public Transform player;
    public Transform reticle;
    public Transform leftIcon;
    public Transform rightIcon;

    private Vector3 leftIconScreenPos;
    private Vector3 rightIconScreenPos;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        // Store initial screen positions of the rotation icons
        leftIconScreenPos = mainCamera.WorldToScreenPoint(leftIcon.position);
        rightIconScreenPos = mainCamera.WorldToScreenPoint(rightIcon.position);
    }

    private void LateUpdate()
    {
        // Convert the stored screen positions back to world space
        Vector3 leftIconWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            leftIconScreenPos.x,
            leftIconScreenPos.y,
            Vector3.Distance(mainCamera.transform.position, leftIcon.position)
        ));

        Vector3 rightIconWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            rightIconScreenPos.x,
            rightIconScreenPos.y,
            Vector3.Distance(mainCamera.transform.position, rightIcon.position)
        ));

        // If reticle is over left rotation icon
        if (Vector3.Distance(reticle.position, leftIcon.position) < 0.1f)
        {
            reticle.position = leftIconWorldPos;
        }
        // If reticle is over right rotation icon
        else if (Vector3.Distance(reticle.position, rightIcon.position) < 0.1f)
        {
            reticle.position = rightIconWorldPos;
        }
    }
}