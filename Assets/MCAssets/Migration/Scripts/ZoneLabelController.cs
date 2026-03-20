// ZoneLabelController.cs
// Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
//
// VERSION:  1.1
// DATE:     2026-03-20
// TIMESTAMP: 2026-03-20T12:00:00Z
//
// CHANGE LOG:
//   v1.1  2026-03-20  DISPLAY NAME SOURCE CHANGE
//     - SetLabel() no longer applies internal PascalCase → spaced formatting.
//       ConstellationManager now passes the resolved display name from
//       ZoneConfig.GetDisplayName() directly. The internal FormatZoneName()
//       helper is retained as a private fallback but is no longer called by default.
//     - This ensures "Motion Sickness" (from ZoneConfig) renders correctly rather
//       than "Vestibular" (from zone.ToString()).
//     - No prefab changes required — API signature unchanged.
//
//   v1.0  2026-03-07  Initial implementation.
//
// OBSOLETE FILES:
//   ZoneLabelController.cs v1.0 (2026-03-07)
//
// PURPOSE:
//   Floating world-space label attached to each zone planet in the Constellation.
//   Fades in when the player gazes near it, faces the camera each frame.
//   Assigned and driven by ConstellationManager — one label per zone planet.
//
// PREFAB SETUP:
//   1. In Project panel: right-click → Create → 3D Object → Text - TextMeshPro
//      (TextMeshPro component, NOT TextMeshProUGUI — no Canvas required).
//   2. Set Font Size: 0.4, Alignment: Centre, Font Style: Bold, Color: white #FFFFFF
//   3. Add a CanvasGroup component to the root GameObject for alpha fade.
//   4. Rename the root GameObject "ZoneLabel".
//   5. Attach this script to the root GameObject.
//   6. Save as a prefab: Assets/Prefabs/ZoneLabel.prefab
//
// USAGE:
//   ConstellationManager instantiates one ZoneLabel prefab per zone planet,
//   positions it above the planet, then calls:
//     label.SetLabel(zoneConfig.GetDisplayName(zone));
//
// FILE LOCATION:
//   Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ZoneLabelController : MonoBehaviour
{
    // ── Settings ──────────────────────────────────────────────────────────────
    [Header("Fade Settings")]
    [Tooltip("Seconds to fade in when becoming visible")]
    [SerializeField] private float _fadeInDuration  = 0.4f;

    [Tooltip("Seconds to fade out when becoming hidden")]
    [SerializeField] private float _fadeOutDuration = 0.6f;

    // ── Private ───────────────────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private string      _labelText;
    private bool        _visible;
    private Coroutine   _fadeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _canvasGroup       = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(Camera.main.transform);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the display text. Pass the fully resolved display name from
    /// ZoneConfig.GetDisplayName(zone) — do not pass zone.ToString().
    /// The string is used as-is; no formatting is applied.
    /// </summary>
    public void SetLabel(string text)
    {
        _labelText = text;

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
        if (_visible) return;
        _visible = true;
        StartFade(1f, _fadeInDuration);
    }

    /// <summary>Fade the label out.</summary>
    public void Hide()
    {
        if (!_visible) return;
        _visible = false;
        StartFade(0f, _fadeOutDuration);
    }

    /// <summary>Show or hide with no fade — use for initial state setup.</summary>
    public void SetVisibleImmediate(bool visible)
    {
        _visible           = visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

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
}
