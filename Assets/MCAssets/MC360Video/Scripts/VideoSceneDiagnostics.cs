using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Diagnostic tool for troubleshooting video scene issues
/// Attach to any GameObject and use context menu to run diagnostics
/// </summary>
public class VideoSceneDiagnostics : MonoBehaviour
{
    [Header("Auto-Find References")]
    [SerializeField] private bool autoFindReferences = true;

    [Header("Manual References (optional)")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform sphere;
    [SerializeField] private Camera camera360;
    [SerializeField] private Camera cameraVR;
    [SerializeField] private GameObject hud;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Monitoring")]
    [SerializeField] private bool monitorContinuously = false;
    [SerializeField] private float monitorInterval = 2f;

    private float lastMonitorTime;

    void Start()
    {
        if (autoFindReferences)
        {
            FindReferences();
        }
    }

    void Update()
    {
        if (monitorContinuously && Time.time - lastMonitorTime > monitorInterval)
        {
            RunFullDiagnostics();
            lastMonitorTime = Time.time;
        }
    }

    [ContextMenu("Find All References")]
    private void FindReferences()
    {
        Debug.Log("[Diagnostics] === Finding References ===");

        // Find Player
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Find Sphere
        if (sphere == null)
        {
            GameObject sphereObj = GameObject.Find("Sphere");
            if (sphereObj != null) sphere = sphereObj.transform;
        }

        // Find Cameras
        if (camera360 == null)
        {
            GameObject cam360 = GameObject.Find("Main Camera360");
            if (cam360 != null) camera360 = cam360.GetComponent<Camera>();
        }

        if (cameraVR == null)
        {
            GameObject camVR = GameObject.Find("Main CameraVR");
            if (camVR != null) cameraVR = camVR.GetComponent<Camera>();
        }

        // Find HUD
        if (hud == null)
        {
            hud = GameObject.Find("HUD");
            if (hud == null) hud = GameObject.Find("VideoHud");
        }

        // Find VideoPlayer
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
        }

        Debug.Log($"[Diagnostics] Player: {(player != null ? "✓" : "✗")}");
        Debug.Log($"[Diagnostics] Sphere: {(sphere != null ? "✓" : "✗")}");
        Debug.Log($"[Diagnostics] Camera360: {(camera360 != null ? "✓" : "✗")}");
        Debug.Log($"[Diagnostics] CameraVR: {(cameraVR != null ? "✓" : "✗")}");
        Debug.Log($"[Diagnostics] HUD: {(hud != null ? "✓" : "✗")}");
        Debug.Log($"[Diagnostics] VideoPlayer: {(videoPlayer != null ? "✓" : "✗")}");
    }

    [ContextMenu("1. Check Camera Positions")]
    public void CheckCameraPositions()
    {
        Debug.Log("=================================================");
        Debug.Log("[Diagnostics] === CAMERA POSITION CHECK ===");
        Debug.Log("=================================================");

        if (sphere != null)
        {
            Debug.Log($"Sphere position: {sphere.position}");
            Debug.Log($"Sphere scale: {sphere.localScale}");
        }
        else
        {
            Debug.LogError("Sphere not found!");
        }

        if (player != null)
        {
            Debug.Log($"Player position: {player.position}");
            Debug.Log($"Player rotation: {player.rotation.eulerAngles}");

            if (sphere != null)
            {
                float playerDist = Vector3.Distance(player.position, sphere.position);
                Debug.Log($"Player distance from sphere: {playerDist:F6} units");

                if (playerDist > 0.01f)
                {
                    Debug.LogWarning("⚠️ PLAYER IS NOT AT SPHERE CENTER!");
                }
                else
                {
                    Debug.Log("✓ Player correctly positioned at sphere center");
                }
            }
        }
        else
        {
            Debug.LogError("Player not found!");
        }

        CheckCamera("Camera360", camera360);
        CheckCamera("CameraVR", cameraVR);

        Debug.Log("=================================================");
    }

    private void CheckCamera(string name, Camera cam)
    {
        if (cam == null)
        {
            Debug.LogWarning($"{name}: Not found!");
            return;
        }

        Debug.Log($"\n--- {name} ---");
        Debug.Log($"Active: {cam.gameObject.activeInHierarchy}");
        Debug.Log($"Enabled: {cam.enabled}");
        Debug.Log($"World position: {cam.transform.position}");
        Debug.Log($"Local position: {cam.transform.localPosition}");
        Debug.Log($"Rotation: {cam.transform.rotation.eulerAngles}");

        if (sphere != null)
        {
            float distance = Vector3.Distance(cam.transform.position, sphere.position);
            Debug.Log($"Distance from sphere center: {distance:F6} units");

            if (distance > 0.01f)
            {
                Debug.LogWarning($"⚠️ {name} IS NOT AT SPHERE CENTER!");
                Debug.LogWarning($"   This will cause video to appear black or distorted!");
            }
            else
            {
                Debug.Log($"✓ {name} correctly centered");
            }
        }

        Debug.Log($"Field of View: {cam.fieldOfView}°");
        Debug.Log($"Near clip: {cam.nearClipPlane}");
        Debug.Log($"Far clip: {cam.farClipPlane}");
        Debug.Log($"Clear flags: {cam.clearFlags}");
        Debug.Log($"Culling mask: {cam.cullingMask}");
    }

    [ContextMenu("2. Check Video Player")]
    public void CheckVideoPlayer()
    {
        Debug.Log("=================================================");
        Debug.Log("[Diagnostics] === VIDEO PLAYER CHECK ===");
        Debug.Log("=================================================");

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not found!");
            Debug.Log("=================================================");
            return;
        }

        Debug.Log($"GameObject: {videoPlayer.gameObject.name}");
        Debug.Log($"Active: {videoPlayer.gameObject.activeInHierarchy}");
        Debug.Log($"Enabled: {videoPlayer.enabled}");
        Debug.Log($"Source: {videoPlayer.source}");
        Debug.Log($"URL: {videoPlayer.url}");
        Debug.Log($"Clip: {videoPlayer.clip}");
        Debug.Log($"Is Playing: {videoPlayer.isPlaying}");
        Debug.Log($"Is Prepared: {videoPlayer.isPrepared}");
        Debug.Log($"Time: {videoPlayer.time:F2} / {videoPlayer.length:F2}");
        Debug.Log($"Frame: {videoPlayer.frame} / {videoPlayer.frameCount}");
        Debug.Log($"Render Mode: {videoPlayer.renderMode}");

        if (videoPlayer.renderMode == VideoRenderMode.MaterialOverride)
        {
            MeshRenderer mr = videoPlayer.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Debug.Log($"Material: {mr.material.name}");
                Debug.Log($"Shader: {mr.material.shader.name}");
                Debug.Log($"Texture: {mr.material.mainTexture}");
            }
        }

        Debug.Log($"Play on Awake: {videoPlayer.playOnAwake}");
        Debug.Log($"Loop: {videoPlayer.isLooping}");
        Debug.Log($"Playback Speed: {videoPlayer.playbackSpeed}");

        // Check for common issues
        if (!videoPlayer.isPlaying && videoPlayer.isPrepared)
        {
            Debug.LogWarning("⚠️ Video is prepared but not playing!");
        }

        if (videoPlayer.isPlaying && string.IsNullOrEmpty(videoPlayer.url) && videoPlayer.clip == null)
        {
            Debug.LogError("⚠️ Video is playing but has no source!");
        }

        Debug.Log("=================================================");
    }

    [ContextMenu("3. Check HUD")]
    public void CheckHUD()
    {
        Debug.Log("=================================================");
        Debug.Log("[Diagnostics] === HUD CHECK ===");
        Debug.Log("=================================================");

        if (hud == null)
        {
            Debug.LogError("HUD not found!");
            Debug.Log("=================================================");
            return;
        }

        Debug.Log($"GameObject: {hud.name}");
        Debug.Log($"Active: {hud.activeInHierarchy}");
        Debug.Log($"Position: {hud.transform.position}");
        Debug.Log($"Local Position: {hud.transform.localPosition}");
        Debug.Log($"Rotation: {hud.transform.rotation.eulerAngles}");
        Debug.Log($"Scale: {hud.transform.localScale}");

        // Check canvas
        Canvas canvas = hud.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"\nCanvas found: {canvas.gameObject.name}");
            Debug.Log($"Render Mode: {canvas.renderMode}");
            Debug.Log($"Enabled: {canvas.enabled}");

            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.Log($"Canvas World Position: {canvas.transform.position}");

                Camera activeCam = camera360 != null && camera360.gameObject.activeInHierarchy ? camera360 : cameraVR;
                if (activeCam != null)
                {
                    float distance = Vector3.Distance(canvas.transform.position, activeCam.transform.position);
                    Debug.Log($"Distance from active camera: {distance:F2} units");

                    if (distance < activeCam.nearClipPlane)
                    {
                        Debug.LogWarning($"⚠️ HUD is closer than camera near clip plane ({activeCam.nearClipPlane})!");
                    }
                    else if (distance > activeCam.farClipPlane)
                    {
                        Debug.LogWarning($"⚠️ HUD is farther than camera far clip plane ({activeCam.farClipPlane})!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No Canvas component found in HUD!");
        }

        Debug.Log("=================================================");
    }

    [ContextMenu("4. Check Active Camera")]
    public void CheckActiveCamera()
    {
        Debug.Log("=================================================");
        Debug.Log("[Diagnostics] === ACTIVE CAMERA CHECK ===");
        Debug.Log("=================================================");

        Camera activeCam = null;
        string activeCamName = "None";

        if (camera360 != null && camera360.gameObject.activeInHierarchy && camera360.enabled)
        {
            activeCam = camera360;
            activeCamName = "Camera360";
        }
        else if (cameraVR != null && cameraVR.gameObject.activeInHierarchy && cameraVR.enabled)
        {
            activeCam = cameraVR;
            activeCamName = "CameraVR";
        }

        Debug.Log($"Active Camera: {activeCamName}");

        if (activeCam != null)
        {
            CheckCamera(activeCamName, activeCam);

            // Check what camera is rendering
            Debug.Log($"\nCamera Tag: {activeCam.tag}");
            Debug.Log($"Target Display: {activeCam.targetDisplay}");
            Debug.Log($"Stereo Enabled: {activeCam.stereoEnabled}");
        }
        else
        {
            Debug.LogError("⚠️ NO ACTIVE CAMERA FOUND!");
            Debug.LogError("This is why the screen is black!");
        }

        // Check Camera.main
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"\nCamera.main: {mainCam.name}");
        }
        else
        {
            Debug.LogWarning("Camera.main is null!");
        }

        Debug.Log("=================================================");
    }

    [ContextMenu("5. RUN FULL DIAGNOSTICS")]
    public void RunFullDiagnostics()
    {
        Debug.Log("\n\n");
        Debug.Log("##################################################");
        Debug.Log("###   VIDEO SCENE FULL DIAGNOSTICS            ###");
        Debug.Log("##################################################");
        Debug.Log($"Time: {Time.time:F2}s");
        Debug.Log($"Frame: {Time.frameCount}");

        if (autoFindReferences)
        {
            FindReferences();
        }

        CheckCameraPositions();
        CheckActiveCamera();
        CheckVideoPlayer();
        CheckHUD();

        Debug.Log("\n##################################################");
        Debug.Log("###   DIAGNOSTICS COMPLETE                    ###");
        Debug.Log("##################################################\n\n");
    }

    [ContextMenu("6. Fix Common Issues")]
    public void AttemptAutoFix()
    {
        Debug.Log("[Diagnostics] === ATTEMPTING AUTO-FIX ===");

        int fixCount = 0;

        // Fix 1: Center player at sphere
        if (player != null && sphere != null)
        {
            float distance = Vector3.Distance(player.position, sphere.position);
            if (distance > 0.01f)
            {
                player.position = sphere.position;
                Debug.Log("✓ Fixed: Moved player to sphere center");
                fixCount++;
            }
        }

        // Fix 2: Reset camera local positions
        if (camera360 != null && camera360.transform.localPosition != Vector3.zero)
        {
            camera360.transform.localPosition = Vector3.zero;
            Debug.Log("✓ Fixed: Reset Camera360 local position");
            fixCount++;
        }

        if (cameraVR != null && cameraVR.transform.localPosition != Vector3.zero)
        {
            cameraVR.transform.localPosition = Vector3.zero;
            Debug.Log("✓ Fixed: Reset CameraVR local position");
            fixCount++;
        }

        // Fix 3: Ensure HUD is active
        if (hud != null && !hud.activeInHierarchy)
        {
            hud.SetActive(true);
            Debug.Log("✓ Fixed: Activated HUD");
            fixCount++;
        }

        // Fix 4: Restart video if it stopped
        if (videoPlayer != null && videoPlayer.isPrepared && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            Debug.Log("✓ Fixed: Restarted video playback");
            fixCount++;
        }

        Debug.Log($"[Diagnostics] Applied {fixCount} fixes");

        if (fixCount > 0)
        {
            Debug.Log("[Diagnostics] Run diagnostics again to verify fixes");
        }
        else
        {
            Debug.Log("[Diagnostics] No issues found to fix automatically");
        }
    }
}