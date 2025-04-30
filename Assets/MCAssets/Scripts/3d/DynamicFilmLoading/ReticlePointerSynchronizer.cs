using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// This component synchronizes the VRReticlePointer with the Unity Event System
// to ensure proper interaction with UI elements and video prefabs
public class ReticlePointerSynchronizer : MonoBehaviour
{
    [SerializeField] private VRReticlePointer reticlePointer;
    [SerializeField] private Camera pointerCamera;
    [SerializeField] private float maxPointerDistance = 15f;
    [SerializeField] private LayerMask interactionLayerMask = -1;  // All layers by default

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = false;
    [SerializeField] private Color debugRayColor = Color.green;

    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        // Find references if not set
        if (reticlePointer == null)
        {
            reticlePointer = GetComponent<VRReticlePointer>();
        }

        if (pointerCamera == null && reticlePointer != null)
        {
            pointerCamera = reticlePointer.GetComponentInChildren<Camera>();
        }

        if (pointerCamera == null)
        {
            pointerCamera = Camera.main;
            Debug.LogWarning("No camera assigned to ReticlePointerSynchronizer, using Main Camera.");
        }

        // Create pointer event data
        pointerEventData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        UpdatePointerPosition();
        CheckForInteraction();
    }

    private void UpdatePointerPosition()
    {
        // Set the pointer position to the center of the screen
        pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
        pointerEventData.delta = Vector2.zero;

        // Clear previous results
        raycastResults.Clear();
    }

    private void CheckForInteraction()
    {
        // First, try Physics raycast for 3D objects (like our video prefabs)
        Ray ray = pointerCamera.ScreenPointToRay(pointerEventData.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPointerDistance, interactionLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if the hit object has an EventTrigger component or EnhancedVideoPlayer
            EventTrigger eventTrigger = hitObject.GetComponent<EventTrigger>();
            EnhancedVideoPlayer videoPlayer = hitObject.GetComponent<EnhancedVideoPlayer>();

            if (eventTrigger != null)
            {
                // Manually trigger pointer enter events
                ExecuteEvents.Execute(hitObject, pointerEventData, ExecuteEvents.pointerEnterHandler);

                // If we have active input (e.g., from VRReticlePointer), trigger click events
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    ExecuteEvents.Execute(hitObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                }
            }
            else if (videoPlayer != null)
            {
                // Direct interaction with video player
                videoPlayer.MouseHoverChangeScene();

                // Check for click to immediately trigger video
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    videoPlayer.SetVideoUrl();
                }
            }
        }
        else
        {
            // If no Physics hit, try UI Raycasting
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            if (raycastResults.Count > 0)
            {
                // Sort by distance
                raycastResults.Sort((x, y) => x.distance.CompareTo(y.distance));

                // Get the first hit UI element
                GameObject firstHitObject = raycastResults[0].gameObject;

                // Execute pointer enter event
                ExecuteEvents.Execute(firstHitObject, pointerEventData, ExecuteEvents.pointerEnterHandler);

                // Check for click
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    ExecuteEvents.Execute(firstHitObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugRay && pointerCamera != null)
        {
            Gizmos.color = debugRayColor;
            Ray ray = pointerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            Gizmos.DrawRay(ray.origin, ray.direction * maxPointerDistance);
        }
    }
}