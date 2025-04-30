using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Component to handle manual adjustment of video objects in the scene
/// </summary>
[RequireComponent(typeof(EnhancedVideoPlayer))]
public class VideoAdjustmentHandler : MonoBehaviour
{
    [Header("Manual Adjustment")]
    [SerializeField] private bool allowManualAdjustment = true;
    [SerializeField] private Vector3 lastSavedPosition;
    [SerializeField] private Quaternion lastSavedRotation;

    private EnhancedVideoPlayer videoPlayer;
    private AdvancedVideoPlacementManager placementManager;
    private bool hasBeenMoved = false;

    private void Awake()
    {
        // Get video player component
        videoPlayer = GetComponent<EnhancedVideoPlayer>();

        // Find the placement manager in the scene
        placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();

        // Store initial position and rotation
        lastSavedPosition = transform.position;
        lastSavedRotation = transform.rotation;
    }

    private void Start()
    {
        // Register with the manager
        if (placementManager != null && videoPlayer != null)
        {
            // Registration will happen automatically when moved
        }
    }

    private void Update()
    {
        // Check if position has changed since last frame
        if (allowManualAdjustment &&
            (transform.position != lastSavedPosition || transform.rotation != lastSavedRotation))
        {
            OnManuallyMoved();
        }
    }

    private void OnManuallyMoved()
    {
        hasBeenMoved = true;

        // Notify placement manager
        if (placementManager != null && videoPlayer != null)
        {
            placementManager.NotifyVideoMoved(gameObject);
        }

        // Update saved position and rotation
        lastSavedPosition = transform.position;
        lastSavedRotation = transform.rotation;
    }

    // Call this to purposely move the video and have it registered
    public void SetPosition(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        OnManuallyMoved();
    }

    // Check if this video has been manually adjusted
    public bool HasBeenManuallyAdjusted()
    {
        return hasBeenMoved;
    }

    // Reset to the last saved position
    public void ResetToLastSavedPosition()
    {
        transform.position = lastSavedPosition;
        transform.rotation = lastSavedRotation;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VideoAdjustmentHandler))]
public class VideoAdjustmentHandlerEditor : Editor
{
    private VideoAdjustmentHandler handler;
    private EnhancedVideoPlayer videoPlayer;

    private void OnEnable()
    {
        handler = (VideoAdjustmentHandler)target;
        videoPlayer = handler.GetComponent<EnhancedVideoPlayer>();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (videoPlayer != null)
        {
            EditorGUILayout.LabelField("Video Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Title", videoPlayer.title);
            EditorGUILayout.LabelField("Zone", videoPlayer.zoneName);
            EditorGUILayout.LabelField("URL", videoPlayer.VideoUrlLink);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Reset Position"))
        {
            if (handler != null)
            {
                handler.ResetToLastSavedPosition();
            }
        }

        EditorGUILayout.HelpBox(
            "This component allows manual adjustment of video position.\n" +
            "Changes are automatically tracked and will be saved in the placement cache.",
            MessageType.Info);
    }
}
#endif