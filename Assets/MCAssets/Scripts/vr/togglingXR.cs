using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR;

public class togglingXR : MonoBehaviour
{
    public bool switchVRon;
    private int vrSetOn;

    [Header("Unity 6 XR Debugging")]
    [SerializeField] private bool enableDetailedLogging = true;

    private void Awake()
    {
        // Unity 6: Force XR initialization check in Awake
        if (enableDetailedLogging)
        {
            Debug.Log("[togglingXR] Awake - checking XR status early");
            StartCoroutine(CheckXRAfterFrame());
        }
    }

    private IEnumerator CheckXRAfterFrame()
    {
        // Wait a few frames to let Unity 6 XR system initialize
        yield return new WaitForSeconds(0.1f);

        if (enableDetailedLogging)
        {
            Debug.Log($"[togglingXR] Post-Awake XR Check:");
            Debug.Log($"[togglingXR] XRGeneralSettings.Instance: {(XRGeneralSettings.Instance != null ? "✅ Available" : "❌ NULL")}");

            if (XRGeneralSettings.Instance != null)
            {
                Debug.Log($"[togglingXR] XR Manager: {(XRGeneralSettings.Instance.Manager != null ? "✅ Available" : "❌ NULL")}");
                if (XRGeneralSettings.Instance.Manager != null)
                {
                    Debug.Log($"[togglingXR] Active Loader: {(XRGeneralSettings.Instance.Manager.activeLoader?.name ?? "None")}");
                }
            }
        }
    }

    public void Start()
    {
        Debug.Log($"[togglingXR] Start method called. Unity version: {Application.unityVersion}");

        // Unity 6: Additional delay to ensure XR system is ready
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        // Give Unity 6 more time to initialize XR
        yield return new WaitForSeconds(0.5f);

        CheckXRStatus();

        int vrState = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[togglingXR] PlayerPrefs toggleToVR at Start: {vrState}");

        if (vrState == 1)
        {
            Debug.Log("[togglingXR] Starting XR from Start method based on PlayerPrefs");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Stopping XR from Start method based on PlayerPrefs");
            StopXR();
        }
    }

    public void SwitchingVR()
    {
        vrSetOn = PlayerPrefs.GetInt("toggleToVR");
        Debug.Log($"[togglingXR] SwitchingVR called. toggleToVR: {vrSetOn}");

        if (vrSetOn == 1)
        {
            Debug.Log("[togglingXR] Starting XR mode.");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Stopping XR mode. Enforcing 2D mode.");
            StopXR();
        }
    }

    private void CheckXRStatus()
    {
        Debug.Log("[togglingXR] === Unity 6 XR Status Check ===");

        // Unity 6: Try multiple approaches to access XR
        bool xrAvailable = false;

        try
        {
            if (XRGeneralSettings.Instance != null)
            {
                Debug.Log("[togglingXR] ✅ XRGeneralSettings.Instance is available");
                xrAvailable = true;

                if (XRGeneralSettings.Instance.Manager != null)
                {
                    Debug.Log("[togglingXR] ✅ XRGeneralSettings.Instance.Manager is available");
                    Debug.Log($"[togglingXR] Active Loader: {(XRGeneralSettings.Instance.Manager.activeLoader?.name ?? "None")}");
                }
                else
                {
                    Debug.LogWarning("[togglingXR] ⚠️ XRGeneralSettings.Instance.Manager is null");
                }
            }
            else
            {
                //Debug.LogError("[togglingXR] ❌ XRGeneralSettings.Instance is null");
            }
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"[togglingXR] Exception checking XR status: {e.Message}");
        }

        if (!xrAvailable)
        {
            //Debug.LogError("[togglingXR] 🔧 XR SETUP ISSUES - Check these settings:");
            //Debug.LogError("[togglingXR] 1. Project Settings → XR Plug-in Management → Android → Google Cardboard ✅");
            //Debug.LogError("[togglingXR] 2. Project Settings → XR Plug-in Management → Initialize XR on Startup ✅");
            //Debug.LogError("[togglingXR] 3. Project Settings → Player → Android → Texture Compression: ETC2 (not ETC)");
            //Debug.LogError("[togglingXR] 4. Restart Unity Editor after changing settings");
        }
    }

    public IEnumerator StartXR()
    {
        Debug.Log("[togglingXR] Attempting to start XR...");

        // Unity 6: Additional retry logic
        int retryCount = 0;
        const int maxRetries = 3;

        while (XRGeneralSettings.Instance == null && retryCount < maxRetries)
        {
            retryCount++;
            //Debug.LogWarning($"[togglingXR] XR not ready, retry {retryCount}/{maxRetries}...");
            yield return new WaitForSeconds(0.5f);
        }

        if (XRGeneralSettings.Instance == null)
        {
            //Debug.LogError("[togglingXR] ❌ CRITICAL: XRGeneralSettings.Instance is still null after retries!");
            //Debug.LogError("[togglingXR] 🔧 SOLUTION: Check XR Plugin Management settings and restart Unity");
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            //Debug.LogError("[togglingXR] ❌ CRITICAL: XRGeneralSettings.Instance.Manager is null!");
            yield break;
        }

        // Check if XR is already initialized
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            Debug.Log($"[togglingXR] ✅ XR already initialized with loader: {XRGeneralSettings.Instance.Manager.activeLoader.name}");

            // Start subsystems if not already running
            if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Debug.Log("[togglingXR] Starting XR subsystems...");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
        }
        else
        {
            Debug.Log("[togglingXR] Initializing XR loader...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("[togglingXR] ❌ Failed to initialize XR loader!");
                Debug.LogError("[togglingXR] 🔧 Check: Google Cardboard enabled in XR Plugin Management → Android");
                yield break;
            }

            Debug.Log($"[togglingXR] ✅ XR initialized successfully with loader: {XRGeneralSettings.Instance.Manager.activeLoader.name}");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Check XR status
        yield return new WaitForEndOfFrame();
        LogXRDeviceStatus();

        Debug.Log("[togglingXR] ✅ XR started successfully!");
    }

    public void StopXR()
    {
        Debug.Log("[togglingXR] Attempting to stop XR...");

        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            Debug.Log("[togglingXR] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            Debug.Log("[togglingXR] ✅ XR subsystems stopped");
        }
        else
        {
            Debug.Log("[togglingXR] No active XR loader to stop");
        }

        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        Debug.Log("[togglingXR] XR stop completed");
    }

    private void LogXRDeviceStatus()
    {
        Debug.Log("[togglingXR] === XR Device Status ===");
        Debug.Log($"[togglingXR] XR Device present: {XRSettings.enabled}");
        Debug.Log($"[togglingXR] XR Device model: {XRSettings.loadedDeviceName}");
        Debug.Log($"[togglingXR] XR Eye texture width: {XRSettings.eyeTextureWidth}");
        Debug.Log($"[togglingXR] XR Eye texture height: {XRSettings.eyeTextureHeight}");

        // Check input devices
        var inputDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevices(inputDevices);
        Debug.Log($"[togglingXR] XR Input devices found: {inputDevices.Count}");
        foreach (var device in inputDevices)
        {
            Debug.Log($"[togglingXR]   - {device.name} ({device.characteristics}) - Valid: {device.isValid}");
        }
    }

    [ContextMenu("Force XR Configuration Check")]
    public void ForceXRConfigurationCheck()
    {
        Debug.Log("[togglingXR] === FORCE XR CONFIGURATION CHECK ===");
        CheckXRStatus();

        // Additional Unity 6 specific checks
#if UNITY_EDITOR
        Debug.Log($"[togglingXR] Build Target: {UnityEditor.EditorUserBuildSettings.activeBuildTarget}");
        Debug.Log($"[togglingXR] Development Build: {UnityEditor.EditorUserBuildSettings.development}");
#endif
    }
}