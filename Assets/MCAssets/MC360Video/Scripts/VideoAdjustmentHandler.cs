using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles manual adjustment of video objects in the scene
/// Notifies the cache system when videos are moved
/// </summary>
public class VideoAdjustmentHandler : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool hasInitialized = false;
    private VideoPlacementCache cacheManager;
    private AdvancedVideoPlacementManager placementManager;
    private EnhancedVideoPlayer videoPlayer;

    private void Start()
    {
        // Store initial position and rotation
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        hasInitialized = true;

        // Get the video player component
        videoPlayer = GetComponent<EnhancedVideoPlayer>();

        // Find the cache manager
        cacheManager = FindObjectOfType<VideoPlacementCache>();
        if (cacheManager == null)
        {
            Debug.LogWarning("No VideoPlacementCache found for position tracking");
        }

        // Find the placement manager
        placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
    }

    private void Update()
    {
        // Check for position or rotation changes
        if (hasInitialized && (transform.position != lastPosition || transform.rotation != lastRotation))
        {
            NotifyPositionChanged();

            // Update stored position and rotation
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }

    private void NotifyPositionChanged()
    {
        // Position has changed, notify the cache manager
        if (cacheManager != null && videoPlayer != null &&
            !string.IsNullOrEmpty(videoPlayer.VideoUrlLink) && !string.IsNullOrEmpty(videoPlayer.zoneName))
        {
            cacheManager.AddOrUpdatePlacementData(
                videoPlayer.VideoUrlLink,
                videoPlayer.zoneName,
                transform.position,
                transform.rotation,
                videoPlayer.prefabType ?? "Default"
            );
        }

        // Also notify the placement manager
        if (placementManager != null)
        {
            placementManager.NotifyVideoMoved(gameObject);
        }
    }

    // This method can be called from the editor to mark this video as moved
    public void MarkAsMoved()
    {
        if (cacheManager == null)
        {
            cacheManager = FindObjectOfType<VideoPlacementCache>();
        }

        if (placementManager == null)
        {
            placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
        }

        NotifyPositionChanged();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VideoAdjustmentHandler))]
    public class VideoAdjustmentHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VideoAdjustmentHandler handler = (VideoAdjustmentHandler)target;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Mark As Moved", GUILayout.Height(30)))
            {
                handler.MarkAsMoved();
            }

            EditorGUILayout.HelpBox(
                "This component tracks position changes to this video object and notifies the cache system. " +
                "You can manually adjust the position in the editor and the changes will be saved.",
                MessageType.Info);
        }

        // Track position changes in the scene view
        private void OnSceneGUI()
        {
            VideoAdjustmentHandler handler = (VideoAdjustmentHandler)target;
            Transform transform = handler.transform;

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(transform.position, transform.rotation);
            Quaternion newRotation = Handles.RotationHandle(transform.rotation, transform.position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(transform, "Move Video");
                transform.position = newPosition;
                transform.rotation = newRotation;

                // Notify the system that the video was moved
                handler.MarkAsMoved();
            }
        }
    }
#endif
}