// ConstellationOrb.cs
// Assets/MCAssets/Migration/Scripts/ConstellationOrb.cs
//
// VERSION:   4.2
// DATE:      2026-04-07
// TIMESTAMP: 2026-04-07T12:00:00Z
//
// CHANGE LOG:
//   v4.2  2026-04-07  PHASE 3 — SIDE ORB RING ROTATION
//     - Added _isFront flag, set true by SetTier(Front), false by all other tiers.
//     - Added sideOrbDwellTime Inspector field (default 1.0s, range 0.3–3.0).
//       Separate from dwellTime (session launch dwell). Side orbs respond faster.
//     - Update() now branches on _isFront:
//         Front orb  — existing behaviour: full dwellTime → Select() → session launch.
//         Side orb   — sideOrbDwellTime → fires onSideOrbSelected (no session launch).
//     - Added onSideOrbSelected UnityEvent. ConstellationManager subscribes at
//       spawn time and calls RotateRingToOrb(zone, orbIndex) on fire.
//     - _orbIndex set by ConstellationManager at spawn so the event carries the
//       correct ring position without searching.
//     - SetTier() now also sets _isFront so the dwell branch is always in sync
//       with the current tier state.
//     - No changes to Select(), session launch, pulse, material, or gaze callbacks.
//
//   v4.1  2026-04-04  ORB TIER SYSTEM — ring navigation support
//     - OrbTier enum: Front, SideNear, SideFar, Hidden.
//     - SetTier(), SetLabelController(), sessionPool, _tierScale.
//
//   v4.0  2026-03-14  Planet mesh support + visual overhaul.
//   v3.0  2026-03-09  MockUserProgress → UserProgressService.
//   v2.0  2026-03-07  Three visible states, GazeHoverTrigger wiring, dwell scale.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationOrb.cs v4.1 (2026-04-04)
//   ConstellationOrb.cs v4.0 (2026-03-14)
//   ConstellationOrb.cs v3.0 (2026-03-09)
//   ConstellationOrb.cs v2.0 (2026-03-07)
//
// DEPENDENCIES:
//   UserProgressService.cs v2.2.0  — OrbState enum, GetOrbState()
//   SessionData.cs                 — sessionID, displayTitle, PhobiaZone enum
//   GazeHoverTrigger.cs            — OnGazeEnter / OnGazeExit callbacks
//   ZoneLabelController.cs         — SetLabel(), Show(), Hide(), SetVisibleImmediate()
//   OrbVisuals.cs                  — EnableEmission() static helper
//   ConstellationManager.cs v3.24  — RotateRingToOrb() subscriber
//
// SCENE SETUP:
//   1. Attach ConstellationOrb and GazeHoverTrigger to the orb prefab root.
//   2. Wire GazeHoverTrigger On Enter → ConstellationOrb.OnGazeEnter
//                             On Exit  → ConstellationOrb.OnGazeExit
//   3. Assign state materials in Inspector (matAvailable, matRecommended,
//      matCompleted, matLocked).
//   4. Optionally assign selectedRing child GameObject.
//   5. session, sessionPool, _orbIndex and onSideOrbSelected listener are all
//      assigned at runtime by ConstellationManager — no manual wiring needed.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConstellationOrb : MonoBehaviour
{
    // ── Tier ──────────────────────────────────────────────────────────────────
    public enum OrbTier { Front, SideNear, SideFar, Hidden }

    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Representative session — assigned by ConstellationManager. Drives state/material.")]
    public SessionData session;

    [Tooltip("Full session pool for this level — orb picks randomly at select time. " +
             "Assigned by ConstellationManager. May contain one or more sessions.")]
    public List<SessionData> sessionPool = new List<SessionData>();

    // ── Gaze / Dwell ─────────────────────────────────────────────────────────
    [Header("Gaze Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("Seconds the front orb must be gazed at before session launch.")]
    public float dwellTime = 3f;

    [Range(0.3f, 3f)]
    [Tooltip("Seconds a side orb must be gazed at before ring rotates to bring it front. " +
             "Shorter than dwellTime — rotation should feel responsive.")]
    public float sideOrbDwellTime = 1.0f;

    // ── Default state materials ───────────────────────────────────────────────
    [Header("Default State Materials  (drag from Assets/Materials/Orbs/)")]
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
    [Tooltip("Front orb scale multiplier relative to base. Default 1.1 = 10% larger.")]
    [Range(1.0f, 1.5f)]
    public float frontScaleMultiplier = 1.1f;

    [Tooltip("Far side orb scale multiplier relative to base. Default 0.9 = 10% smaller.")]
    [Range(0.5f, 1.0f)]
    public float sideFarScaleMultiplier = 0.9f;

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
    [Tooltip("Fired when front orb dwell completes — carries chosen SessionData to ConstellationManager.")]
    public UnityEvent<SessionData> onSessionSelected;

    [Tooltip("Fired when a side orb dwell completes — carries this orb's ring index. " +
             "ConstellationManager subscribes at spawn to call RotateRingToOrb().")]
    public UnityEvent<int> onSideOrbSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer                    _rend;
    private Material                    _defaultMaterial;
    private Material                    _hoverMaterialInst;
    private UserProgressService.OrbState _currentState;
    private ZoneLabelController         _label;

    private bool    _isGazed;
    private float   _gazeTimer;
    private bool    _selected;
    private bool    _isFront;       // true = front orb (session launch), false = side orb (rotate ring)
    private int     _orbIndex;      // position in the ring — set by ConstellationManager at spawn
    private Vector3 _baseScale;
    private Vector3 _tierScale;
    private Coroutine _pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null)
            Debug.LogWarning($"[Orb] {name}: No Renderer found in children.");

        _baseScale = transform.localScale;
        _tierScale = _baseScale;

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
            Debug.LogWarning($"[Orb] {name}: GazeHoverTrigger missing.");
        }

        if (session != null)
            Debug.Log($"[Orb] {name} loaded session: {session.sessionID} pool={sessionPool.Count}");
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

        // Hover scale relative to tier scale, not base
        float targetDwell = _isFront ? dwellTime : sideOrbDwellTime;
        float t = _gazeTimer / targetDwell;
        transform.localScale = _tierScale * Mathf.Lerp(1f, hoverScaleMultiplier, t);

        if (_gazeTimer >= targetDwell)
        {
            if (_isFront)
            {
                Select();
            }
            else
            {
                // Side orb — rotate ring to bring this orb front
                _isGazed   = false;
                _gazeTimer = 0f;
                transform.localScale = _tierScale;
                if (_rend != null && _defaultMaterial != null)
                    _rend.material = _defaultMaterial;

                Debug.Log($"[Orb] Side orb dwell FIRE: index={_orbIndex} session={session?.sessionID}.");
                onSideOrbSelected?.Invoke(_orbIndex);
            }
        }
    }

    // ── Tier API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ConstellationManager when the ring rotates or a zone expands.
    /// Drives scale, label visibility, and _isFront flag.
    /// Hidden tier = SetActive(false).
    /// </summary>
    public void SetTier(OrbTier tier)
    {
        if (tier == OrbTier.Hidden)
        {
            _isFront = false;
            gameObject.SetActive(false);
            Debug.Log($"[Orb] {session?.sessionID} tier=Hidden → deactivated.");
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        switch (tier)
        {
            case OrbTier.Front:
                _isFront   = true;
                _tierScale = _baseScale * frontScaleMultiplier;
                if (_label != null) _label.Show();
                break;

            case OrbTier.SideNear:
                _isFront   = false;
                _tierScale = _baseScale;
                if (_label != null) _label.Hide();
                break;

            case OrbTier.SideFar:
                _isFront   = false;
                _tierScale = _baseScale * sideFarScaleMultiplier;
                if (_label != null) _label.Hide();
                break;
        }

        if (!_isGazed)
            transform.localScale = _tierScale;

        Debug.Log($"[Orb] {session?.sessionID} tier={tier} isFront={_isFront} scale={_tierScale}.");
    }

    /// <summary>
    /// Called by ConstellationManager immediately after SpawnLabel().
    /// Caches the label reference so SetTier can show/hide it.
    /// </summary>
    public void SetLabelController(ZoneLabelController label)
    {
        _label = label;
        Debug.Log($"[Orb] {session?.sessionID} label controller assigned: {label?.gameObject.name ?? "null"}.");
    }

    /// <summary>
    /// Called by ConstellationManager at spawn to record this orb's position in the ring.
    /// Used by onSideOrbSelected so ConstellationManager knows which orb to rotate to.
    /// </summary>
    public void SetOrbIndex(int index)
    {
        _orbIndex = index;
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

        if (debugLogging) Debug.Log($"[Orb] Gaze enter: {session?.sessionID} isFront={_isFront}");
    }

    public void OnGazeExit()
    {
        _isGazed   = false;
        _gazeTimer = 0f;

        transform.localScale = _tierScale;

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
                _tierScale = _baseScale * 0.85f;
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

    // ── Editor gizmo ──────────────────────────────────────────────────────────

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
