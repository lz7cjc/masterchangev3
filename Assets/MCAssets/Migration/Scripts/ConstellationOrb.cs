using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ConstellationOrb v2 — three visible states per orb:
///   DEFAULT   — colour set by OrbState (Available/Recommended/Completed/Locked)
///   HOVER     — overrides to hoverColor while reticle is on the orb, scale grows with dwell
///   SELECTED  — overrides to selectedColor, fires onSessionSelected event
///
/// Wires to GazeReticlePointer via GazeHoverTrigger (must be on same GameObject).
/// Replace material-based state colours with your brand colours in Inspector.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ConstellationOrb : MonoBehaviour
{
    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Drag a SessionData asset here. Assigned automatically by ConstellationManager at runtime.")]
    public SessionData session;

    // ── Gaze / Dwell ─────────────────────────────────────────────────────────
    [Header("Gaze Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("Seconds the user must look at the orb before it selects. Default 3s.")]
    public float dwellTime = 3f;

    // ── Default state materials ───────────────────────────────────────────────
    [Header("Default State Materials  (drag from Assets/Materials/Orbs/)")]
    [Tooltip("Orb is available to play")]
    public Material matAvailable;
    [Tooltip("Orb is recommended next by the system")]
    public Material matRecommended;
    [Tooltip("Orb has been completed by the user")]
    public Material matCompleted;
    [Tooltip("Orb is locked — not yet accessible")]
    public Material matLocked;

    // ── Hover & Selected colours ──────────────────────────────────────────────
    [Header("Hover State  (reticle is over orb, dwell counting)")]
    [Tooltip("Orb tints to this colour while being gazed at. Placeholder: bright white.")]
    public Color hoverColor = new Color(1f, 1f, 1f, 1f);

    [Header("Selected State  (dwell complete, session launching)")]
    [Tooltip("Orb flashes this colour when selected. Placeholder: bright gold.")]
    public Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent<SessionData> onSessionSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Logs gaze enter/exit and selection to Console")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer _rend;
    private Material _defaultMaterial;   // Set by OrbState — restored on gaze exit
    private Material _hoverMaterialInst; // Instance so we can tint without affecting asset
    private MockUserProgress.OrbState _currentState;

    private bool _isGazed;
    private float _gazeTimer;
    private bool _selected;
    private Vector3 _baseScale;
    private Coroutine _pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        _baseScale = transform.localScale;
    }

    void Start()
    {
        // Wire GazeHoverTrigger → this orb.
        // GazeReticlePointer calls OnGazeEnter/OnGazeExit on GazeHoverTrigger,
        // which fires these events. Nothing in GazeReticlePointer needs to change.
        var trigger = GetComponent<GazeHoverTrigger>();
        if (trigger != null)
        {
            trigger.onEnter.AddListener(OnGazeEnter);
            trigger.onExit.AddListener(OnGazeExit);
        }
        else
        {
            Debug.LogWarning($"[Orb] {name}: GazeHoverTrigger component missing — add it to this GameObject.");
        }

        // Log session assignment for debugging (matches your existing log format)
        if (session != null)
            Debug.Log($"[Orb] {name} loaded session: {session.sessionID}");
        else
            Debug.LogWarning($"[Orb] {name} has no SessionData assigned!");

        RefreshState();
    }

    void Update()
    {
        if (_selected) return;
        if (_currentState == MockUserProgress.OrbState.Locked) return;
        if (!_isGazed) return;

        _gazeTimer += Time.deltaTime;

        // Scale orb up gradually as dwell progresses — visual feedback matching templatemousehover
        float t = _gazeTimer / dwellTime;
        transform.localScale = _baseScale * (1f + t * 0.18f);

        if (_gazeTimer >= dwellTime)
            Select();
    }

    // ── Gaze callbacks (called via GazeHoverTrigger events) ──────────────────

    public void OnGazeEnter()
    {
        if (_selected || _currentState == MockUserProgress.OrbState.Locked) return;

        _isGazed = true;
        _gazeTimer = 0f;

        // Stop pulse (if Recommended state) so hover colour is clean
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        // Apply hover colour — tint a material instance so asset is unchanged
        _hoverMaterialInst = new Material(_defaultMaterial);
        SetMaterialColor(_hoverMaterialInst, hoverColor);
        _rend.material = _hoverMaterialInst;

        if (debugLogging) Debug.Log($"[Orb] Gaze enter: {session?.sessionID}");
    }

    public void OnGazeExit()
    {
        _isGazed = false;
        _gazeTimer = 0f;
        transform.localScale = _baseScale;

        // Restore default state material
        if (_defaultMaterial != null)
            _rend.material = _defaultMaterial;

        // Restart pulse if Recommended
        if (_currentState == MockUserProgress.OrbState.Recommended && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseLoop());

        if (debugLogging) Debug.Log($"[Orb] Gaze exit: {session?.sessionID}");
    }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Call after any progress change to update orb visuals.</summary>
    public void RefreshState()
    {
        if (session == null || MockUserProgress.Instance == null) return;

        _currentState = MockUserProgress.Instance.GetOrbState(session.sessionID);
        ApplyDefaultMaterial(_currentState);
    }

    private void ApplyDefaultMaterial(MockUserProgress.OrbState state)
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        switch (state)
        {
            case MockUserProgress.OrbState.Available:
                _defaultMaterial = matAvailable;
                break;
            case MockUserProgress.OrbState.Recommended:
                _defaultMaterial = matRecommended;
                _pulseCoroutine = StartCoroutine(PulseLoop());
                break;
            case MockUserProgress.OrbState.Completed:
                _defaultMaterial = matCompleted;
                transform.localScale = _baseScale * 0.85f;
                break;
            case MockUserProgress.OrbState.Locked:
                _defaultMaterial = matLocked;
                break;
        }

        _rend.material = _defaultMaterial;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void Select()
    {
        if (_selected) return;
        _selected = true;
        _isGazed = false;

        Debug.Log($"[Orb] Selected: {session?.sessionID}");

        // Flash selected colour
        SetMaterialColor(_rend.material, selectedColor);

        onSessionSelected?.Invoke(session);
        StartCoroutine(SelectAnimation());
    }

    private IEnumerator SelectAnimation()
    {
        // Expand then hold
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

    private static void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null) return;
        // URP uses _BaseColor, built-in uses _Color
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