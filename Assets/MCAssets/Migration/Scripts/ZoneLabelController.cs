// ZoneLabelController.cs
// Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
//
// VERSION : 1.8
// DATE    : 2026-04-22
// TIMESTAMP: 2026-04-22T00:00:00Z
//
// CHANGE LOG:
//   v1.8  2026-04-22  USE WORLD-SPACE POSITION NOT LOCAL
//     - LateUpdate: switched from localPosition to world-space position.
//       localPosition Y on a rotated planet moves along the planet's local
//       Y axis — which points downward on Mindfulness (rotation Z:-29 etc).
//       World-space position ignores planet rotation entirely, so the label
//       always appears above the planet in world space regardless of how
//       the planet is rotated.
//     - Offset = planet world position + Vector3.up * (planetRadius * multiplier).
//       planetRadius is already world-space (radius * lossyScale) so no
//       further scale correction needed.
//     - Removed parentScale division (was compensating for wrong approach).
//
//   v1.7  2026-04-22  DIVIDE LOCAL OFFSET BY PARENT SCALE
//     - SetPlanetRadius(float): receives world-space collider radius from
//       ConstellationManager.SpawnLabel(). Stored as _planetRadius.
//     - LateUpdate: localPosition Y = _planetRadius * _radiusMultiplier.
//       This scales the offset proportionally per planet so Heights (r~0.75),
//       WaterWorld (r~0.5), Sharks (r~0.25) all position correctly.
//     - _localOffset Vector3 removed — replaced by _radiusMultiplier float.
//     - Start() no longer reads SphereCollider — radius comes from ConstellationManager.
//
//   v1.5  2026-04-22  SIMPLIFY TO LOCAL POSITION OFFSET
//   v1.4  2026-04-22  BIAS LABEL TOWARD WORLD-UP
//   v1.3  2026-04-22  CAMERA-FACING POSITION ABOVE PLANET
//   v1.2  2026-04-22  FIX MIRRORED TEXT + LIVE OFFSET
//   v1.1  2026-03-22  NULL-SAFE _canvasGroup IN SetVisibleImmediate
//     - Start(): caches SphereCollider from parent to get planet radius.
//       Falls back to lossyScale.x * 0.5f if no collider found.
//     - LateUpdate: positions label along the vector from planet centre
//       toward camera, at distance (radius * _radiusMultiplier). This
//       ensures the label always appears above the visible top of the
//       planet from the camera's POV regardless of planet rotation.
//     - Removed world-space Vector3.up offset — that was wrong for
//       arbitrarily-rotated planets.
//     - _heightOffset replaced by _radiusMultiplier (default 1.3).
//       Increase to push label further from planet surface.
//
//   v1.2  2026-04-22  FIX MIRRORED TEXT + LIVE OFFSET
//
//   v1.1  2026-03-22  NULL-SAFE _canvasGroup IN SetVisibleImmediate
//     - Awake() does not fire on inactive GameObjects in Unity.
//     - Session orb labels are children of inactive orbs at spawn time,
//       so _canvasGroup is null when ConstellationManager calls
//       SetVisibleImmediate() immediately after Instantiate().
//     - EnsureCanvasGroup() helper added: resolves _canvasGroup via
//       GetComponent<CanvasGroup>() if not yet assigned.
//     - Called at the top of SetVisibleImmediate(), SetLabel(), Show(),
//       and Hide() so all public entry points are safe regardless of
//       whether Awake() has run.
//     - Awake() unchanged — still initialises _canvasGroup when the
//       object becomes active normally.
//
//   v1.0  2026-03-07  Initial implementation (replaces empty Unity stub).
//                     Implements S3.5 spec from Setup Guide v6.4.
//
// OBSOLETE FILES — DELETE THESE:
//   ZoneLabelController.cs v1.0 (2026-03-07) — canonical filename, replace in place.
//
// PURPOSE:
//   Floating world-space label attached to each zone orb in the Constellation.
//   Fades in when the player gazes near it, faces the camera each frame.
//   Assigned and driven by ConstellationManager -- one label per zone cluster root.
//
// PREFAB SETUP (S3.5):
//   1. In Project panel: right-click -> Create -> 3D Object -> Text - TextMeshPro
//      This creates a world-space TextMeshPro object (TextMeshPro component,
//      NOT TextMeshProUGUI -- no Canvas required).
//   2. Set Font Size: 0.4, Alignment: Centre, Font Style: Bold, Color: white #FFFFFF
//   3. Add a CanvasGroup component to the root GameObject for alpha fade.
//   4. Rename the root GameObject "ZoneLabel".
//   5. Attach this script (ZoneLabelController.cs) to the root GameObject.
//   6. Save as a prefab: Assets/Prefabs/ZoneLabel.prefab
//
// USAGE:
//   ConstellationManager instantiates one ZoneLabel prefab per zone cluster root,
//   positions it at zoneOrbPosition + Vector3.up * 0.7f, then calls:
//     label.SetLabel(zone.ToString());
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

/// <summary>
/// World-space zone label. Fades in on gaze approach, faces camera each frame.
/// Attach to a GameObject that also has a CanvasGroup component.
/// </summary>
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
    [Tooltip("Label Y offset = planet radius × this value. Tune in Play Mode, set on prefab on exit.")]
    [SerializeField] private float _radiusMultiplier = 1.4f;

    // -- Private --------------------------------------------------------------
    private CanvasGroup  _canvasGroup;
    private string       _labelText;
    private bool         _visible;
    private Coroutine    _fadeCoroutine;
    private float        _planetRadius = 1f;

    // -- Lifecycle ------------------------------------------------------------

    void Awake()
    {
        _canvasGroup       = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        Debug.Log($"[ZoneLabelController] Awake on '{gameObject.name}' — CanvasGroup resolved.");
    }

    void Start()
    {
        Debug.Log($"[ZoneLabelController] '{gameObject.name}' Start — radius={_planetRadius:F3} multiplier={_radiusMultiplier:F2}.");
    }

    void LateUpdate()
    {
        // Position above planet in world space — ignores planet rotation so
        // the label is always above regardless of how the planet is oriented.
        if (transform.parent != null)
        {
            transform.position = transform.parent.position
                                 + Vector3.up * (_planetRadius * _radiusMultiplier);
        }

        // Face camera. TMP 3D meshes render on negative-Z, so Rotate 180° on Y.
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0f, 180f, 0f);
        }
    }

    // -- Helpers (private) ----------------------------------------------------

    /// <summary>
    /// Resolves _canvasGroup if not yet assigned.
    /// Awake() does not run on inactive GameObjects, so any public method
    /// that may be called before the object is first activated must call this first.
    /// </summary>
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
    /// Called by ConstellationManager.SpawnLabel() immediately after instantiation.
    /// Provides the planet's world-space collider radius so localPosition Y
    /// scales correctly for each planet regardless of its scale.
    /// </summary>
    public void SetPlanetRadius(float radius)
    {
        _planetRadius = radius;
        Debug.Log($"[ZoneLabelController] '{gameObject.name}' SetPlanetRadius={radius:F3}.");
    }

    /// <summary>
    /// Set the display text. Called by ConstellationManager after instantiation.
    /// Converts enum names to readable form (e.g. "ClosedSpaces" -> "Closed Spaces").
    /// </summary>
    public void SetLabel(string text)
    {
        EnsureCanvasGroup();
        _labelText = FormatZoneName(text);

        // Find the TextMeshPro component on this or a child object and set text.
        // Using string-based component lookup to avoid a hard TMPro dependency
        // in case TMP is not yet imported -- ConstellationManager should check
        // TMP is installed before calling this.
        var tmp = GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null)
            tmp.text = _labelText;
        else
            Debug.LogWarning($"[ZoneLabelController] TextMeshPro component not found " +
                             $"on {gameObject.name} or its children.");
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
    /// Show or hide with no coroutine fade — safe to call on inactive GameObjects
    /// (e.g. session orb labels spawned before the orb is activated).
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

    /// <summary>
    /// Inserts a space before each capital letter in a PascalCase zone name.
    /// "ClosedSpaces" -> "Closed Spaces", "FoodContamination" -> "Food Contamination"
    /// </summary>
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
