using UnityEngine;

public class PlayerHierarchyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform primaryNav;
    [SerializeField] private Transform secondaryNavs;

    private bool isMoving = false;
    private Vector3 targetPosition;
    private Transform playerRoot;
    private Gazetemplate currentGazeTarget;

    private void Start()
    {
        playerRoot = transform;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        ValidateHierarchy();
    }

    private void ValidateHierarchy()
    {
        if (primaryNav == null)
        {
            primaryNav = transform.Find("PrimaryPanel");
        }

        if (secondaryNavs == null)
        {
            secondaryNavs = transform.Find("SecondaryNavs");
        }

        if (primaryNav == null)
        {
            Debug.LogWarning("PrimaryNav not found in player hierarchy!");
        }

        if (secondaryNavs == null)
        {
            Debug.LogWarning("SecondaryNavs not found in player hierarchy!");
        }
    }

    private void Update()
    {
        // Check if we have a valid gaze target that has completed its delay
        if (currentGazeTarget != null && currentGazeTarget.mouseHover && currentGazeTarget.counter >= currentGazeTarget.Delay)
        {
            if (!isMoving)
            {
                targetPosition = currentGazeTarget.transform.position;
                isMoving = true;
                NotifyMovementStart();
            }
        }

        if (isMoving)
        {
            MoveEntireHierarchy();
        }
    }

    private void MoveEntireHierarchy()
    {
        Vector3 directionToTarget = targetPosition - playerRoot.position;
        directionToTarget.y = 0f; // Keep movement on the horizontal plane

        if (directionToTarget.magnitude <= stoppingDistance)
        {
            isMoving = false;
            NotifyMovementEnd();
            return;
        }

        Vector3 normalizedDirection = directionToTarget.normalized;

        // Move the entire player hierarchy
        playerRoot.position += normalizedDirection * moveSpeed * Time.deltaTime;

        // Rotate the entire hierarchy to face movement direction
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
            playerRoot.rotation = Quaternion.Slerp(
                playerRoot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        UpdateUIElements();
    }

    private void UpdateUIElements()
    {
        Transform hudCanvas = transform.Find("HUD_Canvas");
        if (hudCanvas != null)
        {
            hudCanvas.rotation = Quaternion.LookRotation(hudCanvas.position - mainCamera.transform.position);
        }
    }

    private void NotifyMovementStart()
    {
        Transform noWalk = transform.Find("PrimaryPanel/PrimaryNav/NoWalk");
        if (noWalk != null)
        {
            noWalk.gameObject.SetActive(false);
        }

        Transform currentSpeed = transform.Find("SecondaryNavs/Movement/Currentspeed");
        if (currentSpeed != null)
        {
            currentSpeed.gameObject.SetActive(true);
        }
    }

    private void NotifyMovementEnd()
    {
        Transform noWalk = transform.Find("PrimaryPanel/PrimaryNav/NoWalk");
        if (noWalk != null)
        {
            noWalk.gameObject.SetActive(true);
        }

        Transform currentSpeed = transform.Find("SecondaryNavs/Movement/Currentspeed");
        if (currentSpeed != null)
        {
            currentSpeed.gameObject.SetActive(false);
        }

        // Reset the gaze target
        if (currentGazeTarget != null)
        {
            currentGazeTarget.counter = 0;
            currentGazeTarget = null;
        }
    }

    // Public method to set the current gaze target
    public void SetGazeTarget(Gazetemplate gazeTarget)
    {
        currentGazeTarget = gazeTarget;
    }

    public void StopMovement()
    {
        isMoving = false;
        NotifyMovementEnd();
    }

    public bool IsCurrentlyMoving()
    {
        return isMoving;
    }
}