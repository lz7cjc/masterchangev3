using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Diagnostic tool to help debug VR/XR issues in mobile builds.
/// Attach this to any GameObject in your scene to see detailed XR status.
/// </summary>
public class XRDiagnostics : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showOnScreenLog = true;
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.D; // Press D to toggle display

    private List<string> diagnosticMessages = new List<string>();
    private bool displayEnabled = true;
    private GUIStyle textStyle;

    private void Start()
    {
        // Initialize text style for on-screen display
        textStyle = new GUIStyle();
        textStyle.fontSize = 24;
        textStyle.normal.textColor = Color.white;
        textStyle.wordWrap = true;

        // Run initial diagnostics
        RunDiagnostics();

        // Run diagnostics every 2 seconds
        InvokeRepeating(nameof(RunDiagnostics), 2f, 2f);
    }

    private void Update()
    {
        // Toggle display with key
        if (Input.GetKeyDown(toggleKey))
        {
            displayEnabled = !displayEnabled;
        }
    }

    private void RunDiagnostics()
    {
        diagnosticMessages.Clear();
        diagnosticMessages.Add("=== XR DIAGNOSTICS ===");
        diagnosticMessages.Add($"Time: {System.DateTime.Now:HH:mm:ss}");
        diagnosticMessages.Add("");

        // Check XR General Settings
        CheckXRGeneralSettings();

        // Check XR Manager
        CheckXRManager();

        // Check Active Loader
        CheckActiveLoader();

        // Check XR Subsystems
        CheckXRSubsystems();

        // Check XR Devices
        CheckXRDevices();

        // Check Input
        CheckInput();

        // Check Camera
        CheckCamera();

        // Log to console if enabled
        if (logToConsole)
        {
            string fullLog = string.Join("\n", diagnosticMessages);
            Debug.Log($"[XRDiagnostics]\n{fullLog}");
        }
    }

    private void CheckXRGeneralSettings()
    {
        diagnosticMessages.Add("--- XR General Settings ---");

        if (XRGeneralSettings.Instance == null)
        {
            diagnosticMessages.Add("❌ XRGeneralSettings.Instance is NULL!");
            diagnosticMessages.Add("   Fix: Edit > Project Settings > XR Plug-in Management");
            diagnosticMessages.Add("   Enable XR plugin in Android tab");
            return;
        }

        diagnosticMessages.Add("✓ XRGeneralSettings.Instance exists");
        diagnosticMessages.Add($"   Init on Start: {XRGeneralSettings.Instance.InitManagerOnStart}");
        diagnosticMessages.Add("");
    }

    private void CheckXRManager()
    {
        diagnosticMessages.Add("--- XR Manager ---");

        if (XRGeneralSettings.Instance == null)
        {
            diagnosticMessages.Add("⊘ Cannot check - XRGeneralSettings is null");
            diagnosticMessages.Add("");
            return;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            diagnosticMessages.Add("❌ XRGeneralSettings.Instance.Manager is NULL!");
            diagnosticMessages.Add("   Fix: Check XR Plugin Management settings");
            diagnosticMessages.Add("");
            return;
        }

        diagnosticMessages.Add("✓ XR Manager exists");
        diagnosticMessages.Add($"   Init on Start: {XRGeneralSettings.Instance.InitManagerOnStart}");
        diagnosticMessages.Add("");
    }

    private void CheckActiveLoader()
    {
        diagnosticMessages.Add("--- Active XR Loader ---");

        if (XRGeneralSettings.Instance?.Manager == null)
        {
            diagnosticMessages.Add("⊘ Cannot check - Manager is null");
            diagnosticMessages.Add("");
            return;
        }

        var loader = XRGeneralSettings.Instance.Manager.activeLoader;

        if (loader == null)
        {
            diagnosticMessages.Add("❌ No active XR loader!");
            diagnosticMessages.Add("   Status: XR is NOT running");
            diagnosticMessages.Add("   Fix: Call InitializeLoader() in togglingXR");
            diagnosticMessages.Add("");
            return;
        }

        diagnosticMessages.Add($"✓ Active Loader: {loader.GetType().Name}");
        diagnosticMessages.Add("   Status: XR IS RUNNING");
        diagnosticMessages.Add("");
    }

    private void CheckXRSubsystems()
    {
        diagnosticMessages.Add("--- XR Subsystems ---");

        // Check Display Subsystems
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);

        if (displays.Count == 0)
        {
            diagnosticMessages.Add("❌ No XR Display Subsystems found");
        }
        else
        {
            diagnosticMessages.Add($"✓ Found {displays.Count} Display Subsystem(s)");
            foreach (var display in displays)
            {
                diagnosticMessages.Add($"   - Running: {display.running}");

                // Removed GetRenderFrameRate() call - not available in all Unity versions
                if (display.running)
                {
                    diagnosticMessages.Add($"   - Display active");
                }
            }
        }

        // Check Input Subsystems
        var inputs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputs);

        if (inputs.Count > 0)
        {
            diagnosticMessages.Add($"✓ Found {inputs.Count} Input Subsystem(s)");
        }
        else
        {
            diagnosticMessages.Add("ℹ No Input Subsystems (may be normal)");
        }

        diagnosticMessages.Add("");
    }

    private void CheckXRDevices()
    {
        diagnosticMessages.Add("--- XR Devices ---");

        var devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        if (devices.Count == 0)
        {
            diagnosticMessages.Add("❌ No XR Input Devices detected");
            diagnosticMessages.Add("   (This might be normal for some VR systems)");
        }
        else
        {
            diagnosticMessages.Add($"✓ Found {devices.Count} device(s):");
            foreach (var device in devices)
            {
                diagnosticMessages.Add($"   - {device.name} ({device.characteristics})");
                diagnosticMessages.Add($"     Valid: {device.isValid}");
            }
        }

        diagnosticMessages.Add("");
    }

    private void CheckInput()
    {
        diagnosticMessages.Add("--- Input System ---");

        // Check gyroscope
        if (SystemInfo.supportsGyroscope)
        {
            diagnosticMessages.Add("✓ Device supports gyroscope");

            if (Input.gyro.enabled)
            {
                diagnosticMessages.Add($"   Gyro enabled: Yes");
                diagnosticMessages.Add($"   Gyro attitude: {Input.gyro.attitude}");
            }
            else
            {
                diagnosticMessages.Add("   ⚠ Gyro not enabled - enabling now...");
                Input.gyro.enabled = true;
            }
        }
        else
        {
            diagnosticMessages.Add("❌ Device does NOT support gyroscope!");
            diagnosticMessages.Add("   VR head tracking will not work");
        }

        // Check accelerometer
        if (SystemInfo.supportsAccelerometer)
        {
            diagnosticMessages.Add("✓ Accelerometer supported");
        }
        else
        {
            diagnosticMessages.Add("⚠ Accelerometer NOT supported");
        }

        diagnosticMessages.Add("");
    }

    private void CheckCamera()
    {
        diagnosticMessages.Add("--- Camera Status ---");

        Camera mainCam = Camera.main;

        if (mainCam == null)
        {
            diagnosticMessages.Add("❌ No main camera found!");
        }
        else
        {
            diagnosticMessages.Add($"✓ Main camera: {mainCam.gameObject.name}");
            diagnosticMessages.Add($"   Stereo Enabled: {mainCam.stereoEnabled}");
            diagnosticMessages.Add($"   Stereo Target Eye: {mainCam.stereoTargetEye}");

            if (mainCam.stereoEnabled)
            {
                diagnosticMessages.Add($"   VR: Active (stereo rendering ON)");
            }
            else
            {
                diagnosticMessages.Add($"   VR: Inactive (stereo rendering OFF)");
            }
        }

        diagnosticMessages.Add("");
    }

    private void OnGUI()
    {
        if (!showOnScreenLog || !displayEnabled) return;

        // Create a semi-transparent background
        Rect bgRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
        GUI.Box(bgRect, "");

        // Display messages
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
        GUILayout.BeginVertical();

        foreach (var message in diagnosticMessages)
        {
            GUILayout.Label(message, textStyle);
        }

        GUILayout.Space(20);
        GUILayout.Label($"Press '{toggleKey}' to toggle this display", textStyle);

        // Add manual control buttons
        GUILayout.Space(20);
        if (GUILayout.Button("Refresh Diagnostics", GUILayout.Height(60)))
        {
            RunDiagnostics();
        }

        if (GUILayout.Button("Try Initialize XR", GUILayout.Height(60)))
        {
            TryInitializeXR();
        }

        if (GUILayout.Button("Try Start XR", GUILayout.Height(60)))
        {
            TryStartXR();
        }

        if (GUILayout.Button("Try Stop XR", GUILayout.Height(60)))
        {
            TryStopXR();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void TryInitializeXR()
    {
        Debug.Log("[XRDiagnostics] Attempting to initialize XR...");

        if (XRGeneralSettings.Instance?.Manager != null)
        {
            StartCoroutine(XRGeneralSettings.Instance.Manager.InitializeLoader());
            diagnosticMessages.Add("ℹ Initialized XR loader");
        }
        else
        {
            diagnosticMessages.Add("❌ Cannot initialize - XRGeneralSettings or Manager is null");
        }

        Invoke(nameof(RunDiagnostics), 1f);
    }

    private void TryStartXR()
    {
        Debug.Log("[XRDiagnostics] Attempting to start XR subsystems...");

        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            diagnosticMessages.Add("ℹ Started XR subsystems");
        }
        else
        {
            diagnosticMessages.Add("❌ Cannot start - no active loader. Try Initialize first.");
        }

        Invoke(nameof(RunDiagnostics), 1f);
    }

    private void TryStopXR()
    {
        Debug.Log("[XRDiagnostics] Attempting to stop XR...");

        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            diagnosticMessages.Add("ℹ Stopped XR");
        }
        else
        {
            diagnosticMessages.Add("ℹ XR was not running");
        }

        Invoke(nameof(RunDiagnostics), 1f);
    }
}