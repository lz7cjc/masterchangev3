using UnityEngine;

/// <summary>
/// Manual gyroscope-based head tracking for VR when XR Tracked Pose Driver doesn't work.
/// Attach this to your VR camera alongside (or instead of) Tracked Pose Driver.
/// </summary>
public class ManualGyroscopeTracking : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableTracking = true;
    [SerializeField] private bool debugMode = false;

    [Header("Rotation Settings")]
    [Tooltip("Invert Y axis if head tracking feels upside down")]
    [SerializeField] private bool invertY = false;

    [Tooltip("Invert X axis if head tracking feels reversed")]
    [SerializeField] private bool invertX = false;

    [Tooltip("Sensitivity multiplier for rotation")]
    [Range(0.1f, 2f)]
    [SerializeField] private float sensitivity = 1f;

    [Tooltip("Smooth rotation over time")]
    [SerializeField] private bool smoothRotation = true;

    [Tooltip("Smoothing speed")]
    [Range(1f, 20f)]
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Calibration")]
    [Tooltip("Reset orientation on enable")]
    [SerializeField] private bool resetOnEnable = true;

    [Tooltip("Key to recalibrate (desktop testing)")]
    [SerializeField] private KeyCode recalibrateKey = KeyCode.R;

    private Quaternion baseRotation;
    private Quaternion targetRotation;
    private bool isInitialized = false;

    private void OnEnable()
    {
        Debug.Log("[ManualGyroscopeTracking] OnEnable called");

        if (!SystemInfo.supportsGyroscope)
        {
            Debug.LogError("[ManualGyroscopeTracking] Device does NOT support gyroscope! Disabling script.");
            enabled = false;
            return;
        }

        // Enable gyroscope
        Input.gyro.enabled = true;
        Debug.Log("[ManualGyroscopeTracking] Gyroscope enabled");

        // Wait a frame for gyro to initialize
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        if (resetOnEnable)
        {
            CalibrateOrientation();
        }
        else
        {
            baseRotation = Quaternion.identity;
        }

        isInitialized = true;
        Debug.Log("[ManualGyroscopeTracking] Initialized successfully");

        if (debugMode)
        {
            Debug.Log($"[ManualGyroscopeTracking] Base rotation: {baseRotation.eulerAngles}");
            Debug.Log($"[ManualGyroscopeTracking] Current gyro attitude: {Input.gyro.attitude.eulerAngles}");
        }
    }

    private void Update()
    {
        if (!enableTracking || !isInitialized || !Input.gyro.enabled)
            return;
        
            Debug.Log($"Gyro attitude: {Input.gyro.attitude}");
            // Check for manual recalibration
            if (Input.GetKeyDown(recalibrateKey))
        {
            CalibrateOrientation();
            Debug.Log("[ManualGyroscopeTracking] Manual recalibration triggered");
        }

        // Get gyroscope rotation
        Quaternion gyroRotation = Input.gyro.attitude;

        // Convert gyroscope coordinate system to Unity coordinate system
        // Gyro: X=right, Y=up, Z=forward (left-handed)
        // Unity: X=right, Y=up, Z=forward (right-handed)
        // Need to flip Z and X axes
        Quaternion unityRotation = new Quaternion(
            gyroRotation.x,
            gyroRotation.y,
            -gyroRotation.z,
            -gyroRotation.w
        );

        // Apply 90 degree offset to align with landscape orientation
        Quaternion offsetRotation = Quaternion.Euler(90f, 0f, 0f);
        unityRotation = unityRotation * offsetRotation;

        // Apply base calibration
        targetRotation = baseRotation * unityRotation;

        // Apply inversions if needed
        if (invertY || invertX)
        {
            Vector3 euler = targetRotation.eulerAngles;
            if (invertY) euler.x = -euler.x;
            if (invertX) euler.y = -euler.y;
            targetRotation = Quaternion.Euler(euler);
        }

        // Apply sensitivity
        if (sensitivity != 1f)
        {
            Vector3 euler = targetRotation.eulerAngles;
            euler *= sensitivity;
            targetRotation = Quaternion.Euler(euler);
        }

        // Apply rotation (smooth or instant)
        if (smoothRotation)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            transform.localRotation = targetRotation;
        }

        // Debug logging (only occasionally to avoid spam)
        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[ManualGyroscopeTracking] Gyro: {gyroRotation.eulerAngles}, Unity: {targetRotation.eulerAngles}");
        }
    }

    /// <summary>
    /// Calibrate the base orientation to current device orientation
    /// </summary>
    [ContextMenu("Calibrate Orientation")]
    public void CalibrateOrientation()
    {
        if (!Input.gyro.enabled)
        {
            Debug.LogWarning("[ManualGyroscopeTracking] Cannot calibrate - gyroscope not enabled");
            return;
        }

        // Get current gyro rotation
        Quaternion gyroRotation = Input.gyro.attitude;

        // Convert to Unity coordinate system
        Quaternion unityRotation = new Quaternion(
            gyroRotation.x,
            gyroRotation.y,
            -gyroRotation.z,
            -gyroRotation.w
        );

        // Apply 90 degree offset
        Quaternion offsetRotation = Quaternion.Euler(90f, 0f, 0f);
        unityRotation = unityRotation * offsetRotation;

        // Calculate base rotation to reset orientation
        baseRotation = Quaternion.Inverse(unityRotation);

        Debug.Log("[ManualGyroscopeTracking] Orientation calibrated");
        Debug.Log($"[ManualGyroscopeTracking] New base rotation: {baseRotation.eulerAngles}");
    }

    /// <summary>
    /// Enable or disable head tracking
    /// </summary>
    public void SetTrackingEnabled(bool enabled)
    {
        enableTracking = enabled;
        Debug.Log($"[ManualGyroscopeTracking] Tracking {(enabled ? "enabled" : "disabled")}");

        if (enabled && !Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
        }
    }

    /// <summary>
    /// Reset to neutral orientation
    /// </summary>
    [ContextMenu("Reset Orientation")]
    public void ResetOrientation()
    {
        transform.localRotation = Quaternion.identity;
        baseRotation = Quaternion.identity;
        Debug.Log("[ManualGyroscopeTracking] Orientation reset");
    }

    private void OnDisable()
    {
        Debug.Log("[ManualGyroscopeTracking] OnDisable called");
        isInitialized = false;
    }

    private void OnDestroy()
    {
        // Don't disable gyro on destroy - other scripts might be using it
        Debug.Log("[ManualGyroscopeTracking] OnDestroy called");
    }

    #region Debug Helpers

    [ContextMenu("Toggle Debug Mode")]
    private void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"[ManualGyroscopeTracking] Debug mode: {(debugMode ? "ON" : "OFF")}");
    }

    [ContextMenu("Test Gyroscope Support")]
    private void TestGyroscopeSupport()
    {
        Debug.Log("=== Gyroscope Support Test ===");
        Debug.Log($"Device: {SystemInfo.deviceModel}");
        Debug.Log($"Gyroscope supported: {SystemInfo.supportsGyroscope}");

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log($"Gyroscope enabled: {Input.gyro.enabled}");
            Debug.Log($"Current attitude: {Input.gyro.attitude}");
            Debug.Log($"Current attitude (euler): {Input.gyro.attitude.eulerAngles}");
            Debug.Log($"Update interval: {Input.gyro.updateInterval}");
        }
    }

    #endregion
}