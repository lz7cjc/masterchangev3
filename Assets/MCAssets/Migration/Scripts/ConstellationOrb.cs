// ConstellationOrb.cs
// Assets/MCAssets/Migration/Scripts/ConstellationOrb.cs
//
// VERSION:  4.1
// DATE:     2026-04-02
// TIMESTAMP: 2026-04-02T13:00:00Z
//
// CHANGE LOG:
//   v4.1  2026-04-02  SESSION POOL + RANDOM PICK
//     - sessionPool (List<SessionData>, HideInInspector) added.
//       ConstellationManager assigns the full level pool at spawn.
//     - Select() picks a random session from sessionPool when pool count > 1.
//       Falls back to representative session when pool has 0 or 1 entries.
//     - onSessionSelected now invokes the randomly chosen session, not the representative.
//     - Post-MVP: replace random pick in Select() with a session picker UI call.
//     - OBSOLETE: ConstellationOrb.cs v4.0
//
//   v4.0  2026-03-14  PLANET MESH SUPPORT + VISUAL OVERHAUL
//     - RequireComponent changed from Renderer to nothing — planet models have
//       Renderers on child meshes, so we locate them via GetComponentInChildren.
//     - Hover effect: emissive glow + scale increase (replaces simple tint).
//       Glow is applied by setting _EmissionColor on a material instance.
//       Works with URP Lit and Standard shaders. Enable emission on planet
//       materials in the Inspector (it is OFF by default — tick the Emission
//       checkbox on the material). Inspector field: hoverEmissionColor.
//     - Selected effect: a child ring GameObject (assign in Inspector) is shown
//       and coloured green (#2f9e4f) to replace the gold flash. Ring should be
//       a flat torus or disc mesh, scaled to wrap around the planet's equator.
//       If no ring is assigned, falls back to the previous gold flash.
//     - hoverScaleMultiplier: Inspector-tunable. Default 1.15 (15% larger on hover).
//     - All OrbState references updated to UserProgressService.OrbState.
//     - Backward-compatible: still works on plain sphere GameObjects if a Renderer
//       is on the root — GetComponentInChildren finds it.
//   v3.0  2026-03-09  S4.3 swap — MockUserProgress → UserProgressService.
//   v2.0  2026-03-07  Three visible states, GazeHoverTrigger wiring, dwell scale.
//
// OBSOLETE: ConstellationOrb.cs v3.0, v2.0
//
// SCENE SETUP (planet prefab):
//   1. Replace sphere with your planet 3D model prefab.
//   2. Ensure the planet prefab has a Collider (any shape) at the root or on a
//      child — required for gaze raycast. IsTrigger must be OFF.
//   3. Create a child GameObject called "SelectedRing". Add a torus/disc mesh.
//      Material: solid green #2f9e4f, no emission needed.
//      Scale it so it sits just outside the planet surface (e.g. X/Z = 1.3, Y = 0.05).
//      Assign it to the Selected Ring slot in the Inspector.
//   4. On each planet material, tick the Emission checkbox (URP: enable in
//      Material Inspector). The emission intensity is controlled at runtime.
//   5. Attach ConstellationOrb and GazeHoverTrigger to the root GameObject.
//   6. Wire GazeHoverTrigger On Enter → ConstellationOrb.OnGazeEnter
//              GazeHoverTrigger On Exit  → ConstellationOrb.OnGazeExit

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ConstellationOrb : MonoBehaviour
{
    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Representative session used for state display. Assigned by ConstellationManager.")]
    public SessionData session;

    /// <summary>
    /// All sessions in this level's pool. ConstellationManager assigns this at spawn.
    /// On selection, one session is picked at random from this list.
    /// If empty or null, falls back to session.
    /// </summary>
    [HideInInspector]
    public System.Collections.Generic.List<SessionData> sessionPool;

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

    [Tooltip("How much bigger the planet grows on hover. 1.15 = 15% larger.")]
    [Range(1.0f, 1.5f)]
    public float hoverScaleMultiplier = 1.15f;

    // ── Selected state ────────────────────────────────────────────────────────
    [Header("Selected State  (dwell complete, session launching)")]
    [Tooltip("Child ring GameObject shown when selected. Should be a torus/disc mesh " +
             "scaled to wrap around the planet. Coloured green at runtime. " +
             "If not assigned, falls back to a brief gold flash.")]
    public GameObject selectedRing;

    [Tooltip("Colour applied to the selected ring. Green #2f9e4f by default.")]
    public Color selectedRingColor = new Color(0.18f, 0.62f, 0.31f, 1f); // #2f9e4f

    [Tooltip("Fallback flash colour when no ring is assigned. Gold #FFDA33.")]
    public Color selectedFallbackColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent<SessionData> onSessionSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Logs gaze enter/exit and selection to Console")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer _rend;                    // first renderer found in children
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
        // Planet meshes keep their Renderer on child objects — search entire hierarchy
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null)
            Debug.LogWarning($"[Orb] {name}: No Renderer found in children — planet mesh not set up correctly.");

        _baseScale = transform.localScale;

        // Ensure selected ring starts hidden
        if (selectedRing != null)
        {
            // Tint the ring material at startup
            var ringRend = selectedRing.GetComponent<Renderer>();
            if (ringRend != null)
            {
                var ringMat = new Material(ringRend.sharedMaterial);
                SetMaterialColor(ringMat, selectedRingColor);
                ringRend.material = ringMat;
            }
            selectedRing.SetActive(false);
        }
    }

    void Start()
    {
        var trigger = GetComponent<GazeHoverTrigger>();
        if (trigger != null)
        {
            trigger.onEnter.AddListener(OnGazeEnter);
            trigger.onExit.AddListener(OnGazeExit);
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

        // Grow planet as dwell progresses
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

        // Create a material instance and enable emission for glow
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

        // Restore default material (removes glow)
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

        // Pick randomly from the level pool; fall back to representative session
        SessionData chosen = session;
        if (sessionPool != null && sessionPool.Count > 1)
        {
            int pick = UnityEngine.Random.Range(0, sessionPool.Count);
            chosen = sessionPool[pick];
            Debug.Log($"[Orb] Random pool pick: {chosen.sessionID} " +
                      $"({pick + 1} of {sessionPool.Count}) from {session.sessionID} pool.");
        }
        else
        {
            Debug.Log($"[Orb] Selected (single session): {chosen?.sessionID}");
        }

        if (selectedRing != null)
        {
            selectedRing.SetActive(true);
        }
        else if (_rend != null)
        {
            var mat = new Material(_rend.material);
            SetMaterialColor(mat, selectedFallbackColor);
            _rend.material = mat;
        }

        onSessionSelected?.Invoke(chosen);
        StartCoroutine(SelectAnimation());
    }

    private IEnumerator SelectAnimation()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = _baseScale * (1f + (t / 0.4f) * 0.5f);
            yield return null;
        }
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

    /// <summary>
    /// Enables emission on a material instance and sets the emission colour.
    /// Works with URP Lit (_EmissionColor + keyword) and Standard (_EmissionColor).
    /// The planet's base material must have Emission ticked in the Inspector.
    /// </summary>
    private static void EnableEmission(Material mat, Color color)
    {
        if (mat == null) return;
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", color * 2f); // * 2 for visible brightness in linear space
    }

    /// <summary>Sets base colour on URP Lit or Standard shaders.</summary>
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
