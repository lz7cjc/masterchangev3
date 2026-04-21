// ConstellationOrb.cs
// Assets/MCAssets/Migration/Scripts/ConstellationOrb.cs
//
// VERSION:   4.7
// DATE:      2026-04-21
// TIMESTAMP: 2026-04-21T00:00:00Z
//
// CHANGE LOG:
//   v4.7  2026-04-21  INACTIVE ORB ALPHA + GazeHoverTrigger DISABLE ON INACTIVE
//     - Added [Range(0,1)] public float inactiveOrbAlpha = 0.25f Inspector field.
//       Fallback used when ConstellationManager has no config alpha available.
//       ConstellationManager passes the per-zone config value via SetInteractive(bool, float).
//     - SetInteractive(bool): original single-arg overload now delegates to the
//       new two-arg overload using inactiveOrbAlpha as fallback.
//     - SetInteractive(bool interactive, float alpha): new two-arg overload.
//       When inactive: disables the GazeHoverTrigger component on this GameObject
//       so the physics raycast finds no trigger — reticle stays as a dot without
//       any change to GazeReticlePointer. Also dims the renderer to alpha using a
//       new material instance (does not touch the shared asset). When active:
//       re-enables GazeHoverTrigger and restores _defaultMaterial at full opacity.
//       If _defaultMaterial is null (dummy orb), restores alpha to 1 on current material.
//     - OnGazeExit: material restore guarded by _interactive check. An inactive orb
//       that receives OnGazeExit edge-case will not wipe the dim material.
//     - OBSOLETE: ConstellationOrb.cs v4.6
//
//   v4.6  2026-04-20  ALL ACTIVE BAND ORBS SELECTABLE — INACTIVE BAND NON-INTERACTIVE
//     - Added _interactive bool field. ConstellationManager sets true on active
//       band orbs via SetInteractive(), false on inactive band orbs.
//     - Update(): replaced _isFront check with _interactive guard. All orbs on
//       the active band select on dwell. Inactive orbs ignore gaze entirely.
//     - OnGazeEnter(): added _interactive guard.
//     - Added SetInteractive(bool): cancels in-progress gaze when set false.
//     - Removed sideOrbDwellTime path — all active orbs use dwellTime.
//     - OBSOLETE: ConstellationOrb.cs v4.5
//
//   v4.5  2026-04-19  FIX — BASESCALE ZERO WHEN SetTier CALLED BEFORE Start()
//   v4.4  2026-04-12  FIX — NULL onEnter/onExit IN Start()
//   v4.3  2026-04-12  SYNC WITH ConstellationManager v3.27 — LABEL + BAND + AWAKE GUARDS
//   v4.2  2026-04-07  PHASE 3 — SIDE ORB RING ROTATION
//   v4.1  2026-04-04  ORB TIER SYSTEM
//   v4.0  2026-03-14  Planet mesh support + visual overhaul.
//   v3.0  2026-03-09  MockUserProgress → UserProgressService.
//   v2.0  2026-03-07  Three visible states, GazeHoverTrigger wiring, dwell scale.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationOrb.cs v4.6 (2026-04-20)
//   ConstellationOrb.cs v4.5 (2026-04-19)
//   ConstellationOrb.cs v4.4 (2026-04-12)
//   ConstellationOrb.cs v4.3 (2026-04-12)
//   ConstellationOrb.cs v4.2 (2026-04-07)
//   ConstellationOrb.cs v4.1 (2026-04-04)
//   ConstellationOrb.cs v4.0 (2026-03-14)
//   ConstellationOrb.cs v3.0 (2026-03-09)
//   ConstellationOrb.cs v2.0 (2026-03-07)
//
// DEPENDENCIES:
//   UserProgressService.cs v2.2.0  — OrbState enum, GetOrbState()
//   SessionData.cs                 — sessionID, displayTitle, PhobiaZone enum
//   GazeHoverTrigger.cs            — OnGazeEnter / OnGazeExit callbacks
//   ZoneLabelController.cs v1.1    — SetLabel(), Show(), Hide(), SetVisibleImmediate()
//   OrbVisuals.cs v1.0             — CreateHoverInstance(), EnableEmission()
//   ConstellationManager.cs v3.53  — SetBandInteractive(), SetLevelLabel/SetTitleLabel
//
// SCENE SETUP:
//   All wiring is done at runtime by ConstellationManager — no manual Inspector setup needed.
//   State materials (matAvailable etc.) remain optional Inspector slots for future use.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConstellationOrb : MonoBehaviour
{
    // ── Tier ──────────────────────────────────────────────────────────────────
    public enum OrbTier { Front, SideNear, SideFar, Hidden }

    // ── Band identity ─────────────────────────────────────────────────────────
    [Header("Band Identity")]
    [Tooltip("Set at runtime by ConstellationManager: 'Equator', 'Upper', or 'Lower'.")]
    public string bandName = "Equator";

    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Representative session — assigned by ConstellationManager. Drives state/material.")]
    public SessionData session;

    [Tooltip("Full session pool for this level — orb picks randomly at select time.")]
    public List<SessionData> sessionPool = new List<SessionData>();

    // ── Gaze / Dwell ─────────────────────────────────────────────────────────
    [Header("Gaze Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("Seconds the front orb must be gazed at before session launch.")]
    public float dwellTime = 3f;

    [Range(0.3f, 3f)]
    [Tooltip("Seconds a side orb must be gazed at before ring rotates to bring it front.")]
    public float sideOrbDwellTime = 1.0f;

    // ── Default state materials ───────────────────────────────────────────────
    [Header("Default State Materials")]
    public Material matAvailable;
    public Material matRecommended;
    public Material matCompleted;
    public Material matLocked;

    // ── Hover state ───────────────────────────────────────────────────────────
    [Header("Hover State")]
    [Tooltip("Emissive glow colour on hover. Cyan #56C2D1 by default.")]
    public Color hoverEmissionColor = new Color(0.34f, 0.76f, 0.82f, 1f);

    [Tooltip("Scale multiplier while dwelling. Applied on top of tier scale.")]
    [Range(1.0f, 1.5f)]
    public float hoverScaleMultiplier = 1.15f;

    // ── Tier scale ────────────────────────────────────────────────────────────
    [Header("Tier Scale Multipliers")]
    [Range(1.0f, 1.5f)]
    public float frontScaleMultiplier = 1.1f;

    [Range(0.5f, 1.0f)]
    public float sideFarScaleMultiplier = 0.9f;

    // ── Inactive band visual ──────────────────────────────────────────────────
    [Header("Inactive Band Visual")]
    [Tooltip("Alpha applied to this orb's renderer when it is on a non-active ring. " +
             "ConstellationManager passes the per-zone config value at runtime — " +
             "this field is the fallback if no config value is available.")]
    [Range(0f, 1f)]
    public float inactiveOrbAlpha = 0.25f;

    // ── Selected state ────────────────────────────────────────────────────────
    [Header("Selected State")]
    [Tooltip("Child ring shown when selected. If null, falls back to colour flash.")]
    public GameObject selectedRing;

    [Tooltip("Ring colour. Green #2f9e4f by default.")]
    public Color selectedRingColor = new Color(0.18f, 0.62f, 0.31f, 1f);

    [Tooltip("Fallback flash colour when no ring assigned. Gold #FFDA33.")]
    public Color selectedFallbackColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    [Tooltip("Fired when front orb dwell completes — carries chosen SessionData.")]
    public UnityEvent<SessionData> onSessionSelected;

    [Tooltip("Fired when a side orb dwell completes — carries ring index.")]
    public UnityEvent<int> onSideOrbSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer                     _rend;
    private Material                     _defaultMaterial;
    private Material                     _hoverMaterialInst;
    private UserProgressService.OrbState _currentState;

    // Label controllers — all three wired independently by ConstellationManager
    private ZoneLabelController _label;        // legacy single-label (backward compat)
    private ZoneLabelController _levelLabel;   // "L1", "L2" — shown whenever active
    private ZoneLabelController _titleLabel;   // session.displayTitle — front only

    private bool      _isGazed;
    private float     _gazeTimer;
    private bool      _selected;
    private bool      _isFront;
    private bool      _interactive;   // set by ConstellationManager — true on active band only
    private int       _orbIndex;
    private Vector3   _baseScale;
    private Vector3   _tierScale;
    private Coroutine _pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null)
            Debug.LogWarning($"[Orb] {name}: No Renderer found in children.");

        // _baseScale intentionally NOT captured here.
        // SpawnSlotBand sets localScale after Instantiate() — Awake() runs during
        // Instantiate() and would capture the prefab baked scale (0.5,0.5,0.5).
        // SpawnSlotBand calls Init(spawnedScale) immediately after setting localScale.
        // SetTier() is safe to call only after Init() has been called.

        // Explicitly initialise UnityEvent fields.
        // Unity does NOT initialise them for runtime AddComponent calls — they
        // remain null until after Awake/Start, causing NullReferenceException
        // when ConstellationManager calls AddListener before Start() runs.
        if (onSideOrbSelected == null)
            onSideOrbSelected = new UnityEvent<int>();
        if (onSessionSelected == null)
            onSessionSelected = new UnityEvent<SessionData>();

        if (selectedRing != null)
        {
            var ringRend = selectedRing.GetComponent<Renderer>();
            if (ringRend != null)
            {
                var ringMat = new Material(ringRend.sharedMaterial);
                OrbVisuals.SetBaseColor(ringMat, selectedRingColor);
                ringRend.material = ringMat;
            }
            selectedRing.SetActive(false);
        }

        Debug.Log($"[Orb] Awake: {name} band={bandName}");
    }

    void Start()
    {
        var trigger = GetComponent<GazeHoverTrigger>();
        if (trigger != null)
        {
            // GazeHoverTrigger has no Awake(). For runtime AddComponent usage Unity does
            // not auto-initialise public UnityEvent fields — they remain null until an
            // Awake() runs. Guard both fields here so wiring is safe regardless of
            // whether the trigger was pre-placed in the scene or added at runtime.
            if (trigger.onEnter == null) trigger.onEnter = new UnityEvent();
            if (trigger.onExit  == null) trigger.onExit  = new UnityEvent();
            trigger.onEnter.AddListener(OnGazeEnter);
            trigger.onExit.AddListener(OnGazeExit);
            Debug.Log($"[Orb] {name}: GazeHoverTrigger wired in Start().");
        }
        else
        {
            Debug.LogWarning($"[Orb] {name}: GazeHoverTrigger missing — gaze will not fire.");
        }

        if (session != null)
            Debug.Log($"[Orb] {name}: session={session.sessionID} pool={sessionPool.Count}");
        else
            Debug.Log($"[Orb] {name}: no session assigned (dummy orb — OK until level loads).");

        RefreshState();
    }

    void Update()
    {
        if (_selected) return;
        if (!_interactive) return;   // inactive band — no gaze response
        if (_currentState == UserProgressService.OrbState.Locked) return;
        if (!_isGazed) return;

        _gazeTimer += Time.deltaTime;

        float t = _gazeTimer / dwellTime;
        transform.localScale = _tierScale * Mathf.Lerp(1f, hoverScaleMultiplier, t);

        if (_gazeTimer >= dwellTime)
        {
            Select();
        }
    }

    // ── Tier API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SpawnSlotBand immediately after setting go.transform.localScale.
    /// Captures the correct runtime spawn scale before any SetTier() call can
    /// overwrite it. Must be called before SetTier() or the scale will be zero.
    /// </summary>
    public void Init(Vector3 spawnedScale)
    {
        _baseScale = spawnedScale;
        _tierScale = _baseScale;
        Debug.Log($"[Orb] {name}: Init baseScale={_baseScale.x:F4}.");
    }

    /// <summary>
    /// Called by ConstellationManager when the active band changes.
    /// Delegates to the two-arg overload using the Inspector inactiveOrbAlpha field.
    /// </summary>
    public void SetInteractive(bool interactive)
    {
        SetInteractive(interactive, inactiveOrbAlpha);
    }

    /// <summary>
    /// Called by ConstellationManager with the per-zone config alpha value.
    ///
    /// interactive=false:
    ///   - Disables the GazeHoverTrigger component so the physics raycast finds no
    ///     trigger on this orb — the reticle stays as a dot without any modification
    ///     to GazeReticlePointer.
    ///   - Dims the renderer to the given alpha via a new material instance.
    ///   - Cancels any in-progress gaze.
    ///
    /// interactive=true:
    ///   - Re-enables the GazeHoverTrigger component.
    ///   - Restores _defaultMaterial at full opacity. If _defaultMaterial is null
    ///     (dummy orb not yet state-assigned), restores alpha to 1 on current material.
    /// </summary>
    public void SetInteractive(bool interactive, float alpha)
    {
        _interactive = interactive;

        // ── Enable/disable GazeHoverTrigger ───────────────────────────────────
        // Disabling the component means the raycast in GazeReticlePointer finds
        // no GazeHoverTrigger on this GameObject and treats it as a plain hit
        // with no hover response — reticle remains a dot.
        var trigger = GetComponent<GazeHoverTrigger>();
        if (trigger != null)
        {
            trigger.enabled = interactive;
            Debug.Log($"[Orb] {name}: GazeHoverTrigger enabled={interactive}.");
        }

        if (!interactive)
        {
            // Cancel any in-progress gaze so the orb doesn't select on reactivation.
            _isGazed   = false;
            _gazeTimer = 0f;
            transform.localScale = _tierScale;

            // Restore state material first so we dim from the correct base colour,
            // then apply alpha. This handles the case where a hover material was active.
            if (_rend != null && _defaultMaterial != null)
                _rend.material = _defaultMaterial;

            // Dim via a new material instance — does not touch the shared asset.
            // Also handles dummy orbs where _defaultMaterial is null: dim the
            // current material (which may be a primitive default material).
            if (_rend != null && _rend.material != null)
            {
                var dimMat = new Material(_rend.material);
                Color c = dimMat.HasProperty("_BaseColor")
                    ? dimMat.GetColor("_BaseColor")
                    : dimMat.color;
                c.a = alpha;
                if (dimMat.HasProperty("_BaseColor")) dimMat.SetColor("_BaseColor", c);
                else dimMat.color = c;
                _rend.material = dimMat;
            }

            Debug.Log($"[Orb] {name}: SetInteractive=false alpha={alpha:F2}.");
        }
        else
        {
            // Restore full-opacity state material.
            if (_rend != null)
            {
                if (_defaultMaterial != null)
                {
                    _rend.material = _defaultMaterial;
                    Debug.Log($"[Orb] {name}: SetInteractive=true — _defaultMaterial restored.");
                }
                else
                {
                    // Dummy orb with no state material — just restore alpha to 1.
                    var mat = _rend.material;
                    if (mat != null)
                    {
                        Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                        c.a = 1f;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                        else mat.color = c;
                    }
                    Debug.Log($"[Orb] {name}: SetInteractive=true — no _defaultMaterial, alpha restored.");
                }
            }
        }
    }

    /// <summary>
    /// Called by ConstellationManager when the ring rotates or a zone expands.
    /// Drives scale, label visibility, and _isFront flag.
    /// Hidden tier deactivates the GameObject.
    /// </summary>
    public void SetTier(OrbTier tier)
    {
        if (tier == OrbTier.Hidden)
        {
            _isFront = false;
            // Hide all labels before deactivating so they don't reappear on re-activate
            _levelLabel?.SetVisibleImmediate(false);
            _titleLabel?.SetVisibleImmediate(false);
            if (_label != null) _label.SetVisibleImmediate(false);
            gameObject.SetActive(false);
            Debug.Log($"[Orb] {name} tier=Hidden → deactivated.");
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        switch (tier)
        {
            case OrbTier.Front:
                _isFront   = true;
                _tierScale = _baseScale * frontScaleMultiplier;
                // Level label always visible when active
                _levelLabel?.Show();
                // Title label visible on front only
                _titleLabel?.Show();
                // Legacy single label
                if (_label != null) _label.Show();
                break;

            case OrbTier.SideNear:
                _isFront   = false;
                _tierScale = _baseScale;
                _levelLabel?.Show();
                _titleLabel?.Hide();
                if (_label != null) _label.Hide();
                break;

            case OrbTier.SideFar:
                _isFront   = false;
                _tierScale = _baseScale * sideFarScaleMultiplier;
                _levelLabel?.Show();
                _titleLabel?.Hide();
                if (_label != null) _label.Hide();
                break;
        }

        if (!_isGazed)
            transform.localScale = _tierScale;

        Debug.Log($"[Orb] {name} tier={tier} isFront={_isFront} scale={_tierScale.x:F3}.");
    }

    // ── Label API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wires the level label ("L1", "L2" etc).
    /// Shown whenever the orb is active (not Hidden).
    /// Called by ConstellationManager at spawn.
    /// </summary>
    public void SetLevelLabel(ZoneLabelController label)
    {
        _levelLabel = label;
        if (label != null) label.SetVisibleImmediate(false);
        Debug.Log($"[Orb] {name}: level label assigned → {label?.gameObject.name ?? "null"}.");
    }

    /// <summary>
    /// Wires the session title label.
    /// Shown on Front tier only.
    /// Called by ConstellationManager at spawn.
    /// </summary>
    public void SetTitleLabel(ZoneLabelController label)
    {
        _titleLabel = label;
        if (label != null) label.SetVisibleImmediate(false);
        Debug.Log($"[Orb] {name}: title label assigned → {label?.gameObject.name ?? "null"}.");
    }

    /// <summary>
    /// Legacy single-label API retained for backward compatibility.
    /// Prefer SetLevelLabel / SetTitleLabel for new wiring.
    /// </summary>
    public void SetLabelController(ZoneLabelController label)
    {
        _label = label;
        Debug.Log($"[Orb] {name}: legacy label controller assigned → " +
                  $"{label?.gameObject.name ?? "null"}.");
    }

    /// <summary>
    /// Records this orb's position in the ring.
    /// Used by onSideOrbSelected so ConstellationManager knows which orb to rotate to.
    /// </summary>
    public void SetOrbIndex(int index)
    {
        _orbIndex = index;
        Debug.Log($"[Orb] {name}: orbIndex={index}.");
    }

    // ── Gaze callbacks ────────────────────────────────────────────────────────

    public void OnGazeEnter()
    {
        if (_selected || !_interactive || _currentState == UserProgressService.OrbState.Locked) return;

        _isGazed   = true;
        _gazeTimer = 0f;

        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        // Use OrbVisuals helper — consistent with ZonePlanet hover behaviour
        if (_rend != null)
            _hoverMaterialInst = OrbVisuals.CreateHoverInstance(_rend, hoverEmissionColor);

        if (debugLogging)
            Debug.Log($"[Orb] Gaze enter: {name} session={session?.sessionID} interactive={_interactive}");
    }

    public void OnGazeExit()
    {
        _isGazed   = false;
        _gazeTimer = 0f;

        transform.localScale = _tierScale;

        // Only restore the state material if this orb is on the active band.
        // An inactive orb that receives OnGazeExit (edge case — GazeHoverTrigger is
        // disabled, but belt-and-braces) must not wipe the dim material.
        if (_interactive && _rend != null && _defaultMaterial != null)
            _rend.material = _defaultMaterial;

        if (_currentState == UserProgressService.OrbState.Recommended && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseLoop());

        if (debugLogging)
            Debug.Log($"[Orb] Gaze exit: {name} session={session?.sessionID}");
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
                if (gameObject.activeInHierarchy)
                    _pulseCoroutine = StartCoroutine(PulseLoop());
                break;
            case UserProgressService.OrbState.Completed:
                _defaultMaterial = matCompleted;
                _tierScale       = _baseScale * 0.85f;
                transform.localScale = _tierScale;
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

        SessionData chosen = session;
        if (sessionPool != null && sessionPool.Count > 1)
        {
            chosen = sessionPool[Random.Range(0, sessionPool.Count)];
            Debug.Log($"[Orb] Pool select: {chosen.sessionID} from {sessionPool.Count} options.");
        }

        Debug.Log($"[Orb] Selected: {chosen?.sessionID}");

        if (selectedRing != null)
            selectedRing.SetActive(true);
        else if (_rend != null)
        {
            var mat = new Material(_rend.material);
            OrbVisuals.SetBaseColor(mat, selectedFallbackColor);
            _rend.material = mat;
        }

        onSessionSelected?.Invoke(chosen);
        if (gameObject.activeInHierarchy)
            StartCoroutine(SelectAnimation());
    }

    private IEnumerator SelectAnimation()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = _tierScale * (1f + (t / 0.4f) * 0.5f);
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
                transform.localScale = _tierScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ── Editor gizmo ──────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (session == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f,
            $"{session.sessionID}\n[{bandName}]");
#endif
    }
}
