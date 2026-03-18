// ConstellationOrb.cs
// Assets/MCAssets/Migration/Scripts/ConstellationOrb.cs
//
// VERSION:  6.0
// TIMESTAMP: 2026-03-16T12:00:00Z
//
// CHANGE LOG:
//   v6.0  2026-03-16  EMISSION KEYWORD FIX — hover glow now works on AI FBX materials
//     - Root cause of no-glow in v5.0: MaterialPropertyBlock can write property
//       values but CANNOT enable shader keywords. URP's _EMISSION keyword must be
//       enabled on the material itself; without it the GPU ignores _EmissionColor
//       entirely, regardless of the MPB value. Setting the property block had zero
//       visible effect.
//     - Fix: In Awake(), a runtime material instance is created from the renderer's
//       sharedMaterial. _EMISSION keyword is force-enabled on this instance via
//       mat.EnableKeyword("_EMISSION"). The instance replaces the renderer material.
//       The source sharedMaterial asset is never modified.
//     - MPB then drives _EmissionColor at hover time (keyword already on = GPU
//       reads the colour). Cleared on exit — emission returns to black, no glow.
//     - State material fallback: if no matAvailable/etc assigned, _runtimeMat
//       (the planet's own Diffuse with keyword enabled) stays active.
//   v5.0  2026-03-16  MaterialPropertyBlock hover. Did not fix root issue
//                     (keyword not enabled on source material).
//   v4.0  2026-03-14  Planet mesh support. GetComponentInChildren<Renderer>.
//                     Emission via material instance allocated per gaze event.
//                     SelectedRing. hoverScaleMultiplier.
//   v3.0  2026-03-09  MockUserProgress → UserProgressService swap.
//   v2.0  2026-03-07  Three visible states, GazeHoverTrigger wiring, dwell scale.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationOrb.cs v5.0, v4.0, v3.0, v2.0
//
// PREFAB SETUP (one prefab per zone — each has its own planet FBX):
//   Root GameObject
//     ├─ Sphere Collider  Center(0,0,0)  Radius ~0.005  IsTrigger OFF
//     ├─ ConstellationOrb (this script)
//     ├─ GazeHoverTrigger  OnEnter→OnGazeEnter  OnExit→OnGazeExit
//     ├─ RETOPO (child)  — Mesh Filter + Mesh Renderer with Diffuse material
//     └─ SelectedRing (child, disabled)
//          Cylinder  Scale(0.013, 0.0004, 0.013)  Capsule Collider removed
//          RingMaterial colour #2f9e4f
//          Drag into ConstellationOrb → Selected Ring slot
//
//   NO changes to the Diffuse material are needed. _EMISSION keyword is enabled
//   on a runtime instance in Awake() — the source asset is untouched.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ConstellationOrb : MonoBehaviour
{
    // ── Session ───────────────────────────────────────────────────────────────
    [Header("Session Reference")]
    [Tooltip("Assigned automatically by ConstellationManager at runtime.")]
    public SessionData session;

    // ── Gaze / Dwell ─────────────────────────────────────────────────────────
    [Header("Gaze Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("Seconds of continuous gaze before the planet selects.")]
    public float dwellTime = 3f;

    // ── Default state materials ───────────────────────────────────────────────
    [Header("Default State Materials  (optional — leave None to keep planet's own Diffuse)")]
    [Tooltip("Shown when session is available. Leave None to use the planet's own material.")]
    public Material matAvailable;
    [Tooltip("Shown when session is recommended next.")]
    public Material matRecommended;
    [Tooltip("Shown when session is completed.")]
    public Material matCompleted;
    [Tooltip("Shown when session is locked.")]
    public Material matLocked;

    // ── Hover state ───────────────────────────────────────────────────────────
    [Header("Hover State")]
    [Tooltip("Emission glow colour on hover. Cyan #56C2D1 by default.")]
    public Color hoverEmissionColor = new Color(0.34f, 0.76f, 0.82f, 1f); // #56C2D1

    [Tooltip("Emission brightness multiplier. Increase if glow is hard to see against a bright texture.")]
    [Range(0.5f, 10f)]
    public float hoverEmissionIntensity = 4f;

    [Tooltip("Scale increase on hover. 1.15 = 15% larger.")]
    [Range(1.0f, 1.5f)]
    public float hoverScaleMultiplier = 1.15f;

    // ── Selected state ────────────────────────────────────────────────────────
    [Header("Selected State")]
    [Tooltip("Child SelectedRing GameObject. Shown on selection. If not assigned, falls back to gold flash.")]
    public GameObject selectedRing;

    [Tooltip("Ring colour. Green #2f9e4f.")]
    public Color selectedRingColor = new Color(0.18f, 0.62f, 0.31f, 1f);

    [Tooltip("Fallback flash colour if no ring is assigned.")]
    public Color selectedFallbackColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ── Events ────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent<SessionData> onSessionSelected;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    public bool debugLogging = true;

    // ── Private ───────────────────────────────────────────────────────────────
    private Renderer              _rend;
    private Material              _runtimeMat;   // instance with _EMISSION keyword enabled
    private Material              _defaultMaterial;
    private MaterialPropertyBlock _propBlock;
    private static readonly int   EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private UserProgressService.OrbState _currentState;
    private bool      _isGazed;
    private float     _gazeTimer;
    private bool      _selected;
    private Vector3   _baseScale;
    private Coroutine _pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null)
        {
            Debug.LogWarning($"[Orb] {name}: No Renderer found in children. Check FBX child has Mesh Renderer.");
            return;
        }

        // Create a per-instance copy of the planet material and force-enable the
        // _EMISSION shader keyword. MPB alone cannot toggle keywords — the keyword
        // must live on the material. The sharedMaterial asset is never touched.
        _runtimeMat = new Material(_rend.sharedMaterial);
        _runtimeMat.EnableKeyword("_EMISSION");
        _runtimeMat.SetColor(EmissionColorID, Color.black); // off at rest
        _rend.material = _runtimeMat;

        _propBlock = new MaterialPropertyBlock();
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
            Debug.Log($"[Orb] {name} loaded session: {session.sessionID}");
        else
            Debug.LogWarning($"[Orb] {name}: No SessionData assigned — ConstellationManager assigns this at runtime.");

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

        // _EMISSION keyword already enabled on _runtimeMat (set in Awake).
        // MPB drives the colour — efficient, no allocation.
        if (_rend != null)
        {
            _rend.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorID, hoverEmissionColor * hoverEmissionIntensity);
            _rend.SetPropertyBlock(_propBlock);
        }

        if (debugLogging) Debug.Log($"[Orb] Gaze enter: {session?.sessionID}");
    }

    public void OnGazeExit()
    {
        _isGazed   = false;
        _gazeTimer = 0f;
        transform.localScale = _baseScale;

        // Clear MPB — _EmissionColor returns to black set in Awake. No glow.
        if (_rend != null)
        {
            _propBlock.Clear();
            _rend.SetPropertyBlock(_propBlock);
        }

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

        Material mat = null;
        switch (state)
        {
            case UserProgressService.OrbState.Available:    mat = matAvailable;  break;
            case UserProgressService.OrbState.Recommended:  mat = matRecommended; _pulseCoroutine = StartCoroutine(PulseLoop()); break;
            case UserProgressService.OrbState.Completed:    mat = matCompleted;  transform.localScale = _baseScale * 0.85f; break;
            case UserProgressService.OrbState.Locked:       mat = matLocked;     break;
        }

        // Use state material if assigned; otherwise keep _runtimeMat (planet's Diffuse + keyword)
        _defaultMaterial = (mat != null) ? mat : _runtimeMat;
        _rend.material   = _defaultMaterial;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void Select()
    {
        if (_selected) return;
        _selected = true;
        _isGazed  = false;

        if (_rend != null)
        {
            _propBlock.Clear();
            _rend.SetPropertyBlock(_propBlock);
        }

        Debug.Log($"[Orb] Selected: {session?.sessionID}");

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

        onSessionSelected?.Invoke(session);
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
