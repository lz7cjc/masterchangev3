// ZoneLabelController.cs
// Assets/MCAssets/Migration/Scripts/ZoneLabelController.cs
//
// VERSION : (no version suffix -- canonical filename)
// DATE    : 2026-03-07
//
// CHANGE LOG:
//   2026-03-07  Initial implementation (replaces empty Unity stub).
//               Implements S3.5 spec from Setup Guide v6.4.
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

    // -- Private --------------------------------------------------------------
    private CanvasGroup  _canvasGroup;
    private string       _labelText;
    private bool         _visible;
    private Coroutine    _fadeCoroutine;

    // -- Lifecycle ------------------------------------------------------------

    void Awake()
    {
        _canvasGroup       = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        // Always face the camera so the label is readable from any direction.
        if (Camera.main != null)
            transform.LookAt(Camera.main.transform);
    }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Set the display text. Called by ConstellationManager after instantiation.
    /// Converts enum names to readable form (e.g. "ClosedSpaces" -> "Closed Spaces").
    /// </summary>
    public void SetLabel(string text)
    {
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

    /// <summary>Show or hide with no fade -- use for initial state setup.</summary>
    public void SetVisibleImmediate(bool visible)
    {
        _visible           = visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
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
