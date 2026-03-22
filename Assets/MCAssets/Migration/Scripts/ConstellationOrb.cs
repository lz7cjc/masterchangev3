// ConstellationOrb.cs
// Assets/MCAssets/Migration/Scripts/ConstellationOrb.cs
//
// VERSION:  6.1
// TIMESTAMP: 2026-03-22T00:00:00Z
//
// CHANGE LOG:
//   v6.1  2026-03-22  SELECTION FLASH ANIMATION
//     - Flash on selection is now a proper coroutine: emission ramps up to peak
//       then fades back to zero, then restores the default material.
//     - Three new Inspector fields under "Selected State":
//         flashColor       — colour of the emission flash (default gold #FFDA33)
//         flashPeakIntensity — emission multiplier at peak (default 6)
//         flashDuration    — total duration in seconds (default 0.5)
//     - selectedFallbackColor removed (replaced by flashColor).
//     - Flash runs whether or not selectedRing is assigned; ring and flash are
//       now independent — ring shows if assigned, flash always plays.
//     - SelectAnimation (scale-only coroutine) merged into FlashAnimation so
//       both effects run in a single coroutine.
//   v4.0  2026-03-14  PLANET MESH SUPPORT + VISUAL OVERHAUL
//   v3.0  2026-03-09  S4.3 swap — MockUserProgress → UserProgressService.
//   v2.0  2026-03-07  Three visible states, GazeHoverTrigger wiring, dwell scale.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationOrb.cs v4.0 (2026-03-14)
//   ConstellationOrb.cs v3.0 (2026-03-09)
//   ConstellationOrb.cs v2.0 (2026-03-07)
//
// SCENE SETUP (planet prefab):
//   1. Replace sphere with your planet 3D model prefab.
//   2. Ensure the planet prefab has a Collider (any shape) at the root or on a
//      child — required for gaze raycast. IsTrigger must be OFF.
//   3. On each planet material, tick the Emission checkbox (URP: enable in
//      Material Inspector). The emission intensity is controlled at runtime.
//   4. Attach ConstellationOrb and GazeHoverTrigger to the root GameObject.
//   5. Wire GazeHoverTrigger On Enter → ConstellationOrb.OnGazeEnter
//              GazeHoverTrigger On Exit  → ConstellationOrb.OnGazeExit
//   6. (Optional) Assign a SelectedRing child GameObject — shown alongside flash.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ConstellationOrb : MonoBehaviour
{
    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Drag a SessionData asset here. Assigned automatically by ConstellationManager at runtime.")]
    public SessionData session;

    // ── Gaze / Dwell ─────────────────────────────────────────────────────────
    [Header("Gaze Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("Seconds the user must look at the planet before it selects.")]
    public float dwellTime = 3f;

    // ── Default state materials ───────────────────────────────────────────────
    [Header("Default State Materials  (drag from Assets/Materials/Orbs/)")]
    [Tooltip("Planet is available to play")]
    public Material matAvailable;
    [Tooltip("Planet is recommended next by the system")]
    public Material matRecommended;
    [Tooltip("Planet has been completed by the user")]
    public Material matCompleted;
    [Tooltip("Planet is locked — not yet accessible")]
    public Material matLocked;

    // ── Hover state ───────────────────────────────────────────────────────────
    [Header("Hover State  (reticle is over planet, dwell counting)")]
    [Tooltip("Emissive glow colour applied when gazed at. Cyan #56C2D1 by default.")]
    public Color hoverEmissionColor = new Color(0.34f, 0.76f, 0.82f, 1f); // #56C2D1

    [Tooltip("How much bigger the planet grows on hover. 1.15 = 15% larger. Set to 1 for no change.")]
    [Range(1.0f, 1.5f)]
    public float hoverScaleMultiplier = 1.15f;

    // ── Selected state ────────────────────────────────────────────────────────
    [Header("Selected State  (dwell complete, session launching)")]
    [Tooltip("Child ring GameObject shown when selected. Optional — leave empty to skip ring.")]
    public GameObject selectedRing;

    [Tooltip("Colour applied to the selected ring. Green #2f9e4f by default.")]
    public Color selectedRingColor = new Color(0.18f, 0.62f, 0.31f, 1f); // #2f9e4f

    [Tooltip("Emission colour of the selection flash. Gold #FFDA33 by default.")]
    public Color flashColor = new Color(1f, 0.85f, 0.2f, 1f); // #FFDA33

    [Tooltip("Peak emission brightness during the flash. Higher = more intense.")]
    [Range(1f, 20f)]
    public float flashPeakIntensity = 6f;

    [Tooltip("Total duration of the flash in seconds. Ramps up then fades out.")]
    [Range(0.1f, 2f)]
    public float flashDuration = 0.5f;

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent<SessionData> onSessionSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Logs gaze enter/exit and selection to Console")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer _rend;
    private Material _defaultMaterial;
    private Material _hoverMaterialInst;
    private UserProgressService.OrbState _currentState;

    private bool _isGazed;
    private float _gazeTimer;
    private bool _selected;
    private Vector3 _baseScale;
    private Coroutine _pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null)
            Debug.LogWarning($"[Orb] {name}: No Renderer found in children — planet mesh not set up correctly.");

        _baseScale = transform.localScale;

        if (selectedRing != null)
        {
            var ringRend = selectedRing.GetComponent<Renderer>();
            if (ringRend != null)
            {
                var ringMat = new Material(ringRend.sharedMaterial);
                SetMaterialColor(ringMat, selectedRingColor);
                ringRend.material = ringMat;
            }
            selectedRing.SetActive(false);
        }

        Debug.Log($"[Orb] Awake: {name} — renderer={((_rend != null) ? _rend.name : "MISSING")}");
    }

    void Start()
    {
        var trigger = GetComponent<GazeHoverTrigger>();
        if (trigger != null)
        {
            trigger.onEnter.AddListener(OnGazeEnter);
            trigger.onExit.AddListener(OnGazeExit);
            Debug.Log($"[Orb] {name}: GazeHoverTrigger wired.");
        }
        else
        {
            Debug.LogWarning($"[Orb] {name}: GazeHoverTrigger component missing.");
        }

        if (session != null)
            Debug.Log($"[Orb] {name} loaded session: {session.sessionID}");
        else
            Debug.LogWarning($"[Orb] {name} has no SessionData assigned!");

        RefreshState();
    }

    void Update()
    {
        if (_selected) return;
        if (_currentState == UserProgressService.OrbState.Locked) return;
        if (!_isGazed) return;

        _gazeTimer += Time.deltaTime;

        float t = _gazeTimer / dwellTime;
        transform.localScale = _baseScale * Mathf.Lerp(1f, hoverScaleMultiplier, t);

        if (_gazeTimer >= dwellTime)
            Select();
    }

    // ── Gaze callbacks ────────────────────────────────────────────────────────

    public void OnGazeEnter()
    {
        if (_selected || _currentState == UserProgressService.OrbState.Locked) return;

        _isGazed   = true;
        _gazeTimer = 0f;

        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        if (_rend != null)
        {
            _hoverMaterialInst = new Material(_rend.material);
            EnableEmission(_hoverMaterialInst, hoverEmissionColor);
            _rend.material = _hoverMaterialInst;
        }

        if (debugLogging) Debug.Log($"[Orb] Gaze enter: {session?.sessionID}");
    }

    public void OnGazeExit()
    {
        _isGazed   = false;
        _gazeTimer = 0f;
        transform.localScale = _baseScale;

        if (_rend != null && _defaultMaterial != null)
            _rend.material = _defaultMaterial;

        if (_currentState == UserProgressService.OrbState.Recommended && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseLoop());

        if (debugLogging) Debug.Log($"[Orb] Gaze exit: {session?.sessionID}");
    }

    // ── State ─────────────────────────────────────────────────────────────────

    public void RefreshState()
    {
        if (session == null || UserProgressService.Instance == null) return;

        _currentState = UserProgressService.Instance.GetOrbState(session.sessionID);
        ApplyDefaultMaterial(_currentState);
        Debug.Log($"[Orb] RefreshState: {session.sessionID} → {_currentState}");
    }

    private void ApplyDefaultMaterial(UserProgressService.OrbState state)
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        if (_rend == null) return;

        switch (state)
        {
            case UserProgressService.OrbState.Available:
                _defaultMaterial = matAvailable;
                break;
            case UserProgressService.OrbState.Recommended:
                _defaultMaterial = matRecommended;
                _pulseCoroutine  = StartCoroutine(PulseLoop());
                break;
            case UserProgressService.OrbState.Completed:
                _defaultMaterial = matCompleted;
                transform.localScale = _baseScale * 0.85f;
                break;
            case UserProgressService.OrbState.Locked:
                _defaultMaterial = matLocked;
                break;
        }

        if (_defaultMaterial != null)
            _rend.material = _defaultMaterial;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void Select()
    {
        if (_selected) return;
        _selected = true;
        _isGazed  = false;

        Debug.Log($"[Orb] Selected: {session?.sessionID} — starting flash (colour={flashColor}, " +
                  $"peakIntensity={flashPeakIntensity}, duration={flashDuration}s)");

        if (selectedRing != null)
            selectedRing.SetActive(true);

        onSessionSelected?.Invoke(session);
        StartCoroutine(FlashAnimation());
    }

    /// <summary>
    /// Emission flash + scale bump on selection.
    /// First half of flashDuration ramps emission up to flashPeakIntensity.
    /// Second half fades emission back to zero, then restores default material.
    /// Scale bumps to 1.5× over the same duration then snaps back.
    /// </summary>
    private IEnumerator FlashAnimation()
    {
        if (_rend == null)
        {
            Debug.LogWarning($"[Orb] FlashAnimation: no renderer on {name} — flash skipped.");
            yield break;
        }

        // Create a fresh material instance for the flash so we never dirty the default
        Material flashMat = new Material(_rend.sharedMaterial != null ? _rend.sharedMaterial : _rend.material);
        flashMat.EnableKeyword("_EMISSION");
        _rend.material = flashMat;

        float half    = flashDuration * 0.5f;
        float elapsed = 0f;

        // Ramp up
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            float intensity = Mathf.Lerp(0f, flashPeakIntensity, t);
            if (flashMat.HasProperty("_EmissionColor"))
                flashMat.SetColor("_EmissionColor", flashColor * intensity);
            transform.localScale = _baseScale * Mathf.Lerp(1f, 1.5f, t);
            yield return null;
        }

        Debug.Log($"[Orb] FlashAnimation: {session?.sessionID} peak reached.");

        elapsed = 0f;

        // Ramp down
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            float intensity = Mathf.Lerp(flashPeakIntensity, 0f, t);
            if (flashMat.HasProperty("_EmissionColor"))
                flashMat.SetColor("_EmissionColor", flashColor * intensity);
            transform.localScale = _baseScale * Mathf.Lerp(1.5f, 1f, t);
            yield return null;
        }

        // Restore default material
        if (_defaultMaterial != null)
            _rend.material = _defaultMaterial;

        transform.localScale = _baseScale;

        Debug.Log($"[Orb] FlashAnimation: {session?.sessionID} complete — default material restored.");
    }

    // ── Pulse (Recommended state) ─────────────────────────────────────────────

    private IEnumerator PulseLoop()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.localScale = _baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnableEmission(Material mat, Color color)
    {
        if (mat == null) return;
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", color * 2f);
    }

    private static void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }

    // ── Editor gizmo ─────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (session == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, session.sessionID);
#endif
    }
}
