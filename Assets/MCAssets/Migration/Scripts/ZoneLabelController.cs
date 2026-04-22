// ZoneLabelController.cs
// Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
//
// VERSION : 2.0
// DATE    : 2026-04-22
// TIMESTAMP: 2026-04-22T12:00:00Z
//
// CHANGE LOG:
//   v2.0  2026-04-22  FONT SIZE + ROTATION CONTROL
//     - Added SetFontSize(float): sets TMPro.TextMeshPro.fontSize on the TMP
//       component. Called by ConstellationManager.SpawnLabel() with
//       OrbLayoutConfig.labelFontSize. Per-planet font size without touching prefab.
//     - Added SetLabelRotation(Vector3 euler): stores world-space euler for the label.
//       Applied in LateUpdate after LookAt so it overrides camera-facing when set.
//       When _rotationOverride is false (default), behaviour is unchanged — label
//       faces camera as before. When true, stored euler is applied instead.
//     - Added GetLabelRotation(): returns current _worldRotation euler for
//       OrbLayoutEditor to read the live value and save it.
//     - _rotationOverride bool: false by default. Set true by SetLabelRotation()
//       when the euler is non-zero; set false when zero vector is passed so camera-
//       facing behaviour auto-restores when the override is cleared.
//     - OBSOLETE: ZoneLabelController.cs v1.9
//
//   v1.9  2026-04-22  PER-ZONE LABEL OFFSET FROM CONFIG
//     - Added SetLabelOffset(Vector3), GetLabelOffset().
//     - LateUpdate: transform.position = parent.position + _localOffset.
//     - _radiusMultiplier removed. Position driven entirely by _localOffset.
//     - SetPlanetRadius retained as no-op for backward compatibility.
//
//   v1.8  2026-04-22  USE WORLD-SPACE POSITION NOT LOCAL
//   v1.7  2026-04-22  DIVIDE LOCAL OFFSET BY PARENT SCALE
//   v1.5  2026-04-22  SIMPLIFY TO LOCAL POSITION OFFSET
//   v1.4  2026-04-22  BIAS LABEL TOWARD WORLD-UP
//   v1.3  2026-04-22  CAMERA-FACING POSITION ABOVE PLANET
//   v1.2  2026-04-22  FIX MIRRORED TEXT + LIVE OFFSET
//   v1.1  2026-03-22  NULL-SAFE _canvasGroup IN SetVisibleImmediate
//   v1.0  2026-03-07  Initial implementation.
//
// OBSOLETE FILES:
//   ZoneLabelController.cs v1.9 (2026-04-22)
//
// PURPOSE:
//   Floating world-space label attached to each zone planet in the Constellation.
//   Fades in when the planet is expanded, faces the camera each frame (unless
//   rotation override is active). Font size and position are per-planet via config.
//
// FILE LOCATION:
//   Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ZoneLabelController : MonoBehaviour
{
    // -- Settings -------------------------------------------------------------
    [Header("Fade Settings")]
    [Tooltip("Seconds to fade in when becoming visible")]
    [SerializeField] private float _fadeInDuration  = 0.4f;

    [Tooltip("Seconds to fade out when becoming hidden")]
    [SerializeField] private float _fadeOutDuration = 0.6f;

    [Header("Position")]
    [Tooltip("World-space offset from planet centre. Adjust via Orb Layout Editor scene handle.")]
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 1f, 0f);

    [Header("Rotation")]
    [Tooltip("World-space euler rotation override. Zero = face camera (default). " +
             "Set via Orb Layout Editor scene handle or SetLabelRotation().")]
    [SerializeField] private Vector3 _worldRotation = Vector3.zero;

    // -- Private --------------------------------------------------------------
    private CanvasGroup  _canvasGroup;
    private string       _labelText;
    private bool         _visible;
    private Coroutine    _fadeCoroutine;
    private bool         _rotationOverride = false;  // true when _worldRotation is non-zero

    // -- Lifecycle ------------------------------------------------------------

    void Awake()
    {
        _canvasGroup       = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        Debug.Log($"[ZoneLabelController] Awake on '{gameObject.name}' — CanvasGroup resolved.");
    }

    void Start()
    {
        // Apply any serialised rotation override from config
        _rotationOverride = _worldRotation != Vector3.zero;
        Debug.Log($"[ZoneLabelController] '{gameObject.name}' Start — offset={_localOffset} rotation={_worldRotation} rotOverride={_rotationOverride}.");
    }

    void LateUpdate()
    {
        // Position: planet centre + world-space offset. Ignores planet rotation.
        if (transform.parent != null)
            transform.position = transform.parent.position + _localOffset;

        if (_rotationOverride)
        {
            // Apply saved world-space euler directly — ignores camera facing.
            transform.rotation = Quaternion.Euler(_worldRotation);
        }
        else
        {
            // Default: face camera. TMP 3D meshes render on negative-Z, rotate 180° Y.
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    // -- Private helpers ------------------------------------------------------

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null) return;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            Debug.LogError($"[ZoneLabelController] CanvasGroup missing on '{gameObject.name}'. " +
                           "Add a CanvasGroup component to the ZoneLabel prefab root.");
    }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Sets the world-space position offset from planet centre.
    /// Called by ConstellationManager.SpawnLabel() with OrbLayoutConfig.labelOffset.
    /// </summary>
    public void SetLabelOffset(Vector3 offset)
    {
        _localOffset = offset;
        Debug.Log($"[ZoneLabelController] '{gameObject.name}' SetLabelOffset={offset}.");
    }

    /// <summary>Returns the current world-space position offset.</summary>
    public Vector3 GetLabelOffset() => _localOffset;

    /// <summary>
    /// Sets a world-space euler rotation override for this label.
    /// Passing Vector3.zero restores camera-facing behaviour.
    /// Called by ConstellationManager.SpawnLabel() with OrbLayoutConfig.labelRotation.
    /// </summary>
    public void SetLabelRotation(Vector3 euler)
    {
        _worldRotation    = euler;
        _rotationOverride = (euler != Vector3.zero);
        Debug.Log($"[ZoneLabelController] '{gameObject.name}' SetLabelRotation={euler} override={_rotationOverride}.");
    }

    /// <summary>Returns the current world-space euler rotation override.</summary>
    public Vector3 GetLabelRotation() => _worldRotation;

    /// <summary>
    /// Sets the TextMeshPro font size for this label.
    /// Called by ConstellationManager.SpawnLabel() with OrbLayoutConfig.labelFontSize.
    /// </summary>
    public void SetFontSize(float size)
    {
        var tmp = GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null)
        {
            tmp.fontSize = size;
            Debug.Log($"[ZoneLabelController] '{gameObject.name}' SetFontSize={size}.");
        }
        else
            Debug.LogWarning($"[ZoneLabelController] SetFontSize: TextMeshPro not found on '{gameObject.name}'.");
    }

    /// <summary>Retained for backward compatibility — no longer used.</summary>
    public void SetPlanetRadius(float radius) { }

    /// <summary>
    /// Set the display text. Called by ConstellationManager after instantiation.
    /// Converts PascalCase zone names to readable form (e.g. "ClosedSpaces" → "Closed Spaces").
    /// </summary>
    public void SetLabel(string text)
    {
        EnsureCanvasGroup();
        _labelText = FormatZoneName(text);
        var tmp = GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null)
            tmp.text = _labelText;
        else
            Debug.LogWarning($"[ZoneLabelController] TextMeshPro not found on {gameObject.name}.");
    }

    /// <summary>Fade the label in.</summary>
    public void Show()
    {
        EnsureCanvasGroup();
        if (_visible) return;
        _visible = true;
        StartFade(1f, _fadeInDuration);
    }

    /// <summary>Fade the label out.</summary>
    public void Hide()
    {
        EnsureCanvasGroup();
        if (!_visible) return;
        _visible = false;
        StartFade(0f, _fadeOutDuration);
    }

    /// <summary>
    /// Show or hide immediately with no coroutine — safe on inactive GameObjects.
    /// </summary>
    public void SetVisibleImmediate(bool visible)
    {
        EnsureCanvasGroup();
        _visible = visible;
        if (_canvasGroup != null)
            _canvasGroup.alpha = visible ? 1f : 0f;
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        Debug.Log($"[ZoneLabelController] SetVisibleImmediate({visible}) on '{gameObject.name}'.");
    }

    // -- Fade -----------------------------------------------------------------

    private void StartFade(float targetAlpha, float duration)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(targetAlpha, duration));
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start   = _canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = target;
        _fadeCoroutine     = null;
    }

    // -- Helpers --------------------------------------------------------------

    private static string FormatZoneName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}
