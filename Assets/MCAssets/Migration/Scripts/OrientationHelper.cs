// OrientationHelper.cs
// Assets/MCAssets/Migration/Scripts/OrientationHelper.cs
//
// VERSION:  2.0
// DATE:     2026-03-15
// TIMESTAMP: 2026-03-15T00:00:00Z
//
// CHANGE LOG:
//   v2.0  2026-03-15  DYNAMIC PLANET COUNT MESSAGE
//     - Prompt text is now dynamic: "X planets are waiting — look around to find them all."
//       where X = ConstellationManager.Instance.ZoneClusterCount.
//     - New Inspector slot: promptText (TextMeshPro) — drag the text component here.
//       If not assigned, the prompt still displays but text cannot be updated dynamically.
//     - Text is set in Start() after one frame wait so ConstellationManager has
//       finished BuildConstellation() and ZoneClusterCount is accurate.
//     - Backward-compatible with v1 scene setup — new promptText slot is optional.
//     - OBSOLETE: OrientationHelper.cs v1 (2026-03-07)
//
//   v1  2026-03-07  Initial implementation. Static message.
//
// OBSOLETE FILES:
//   OrientationHelper.cs v1 (2026-03-07)
//
// SCENE SETUP (v2):
//   Steps 1–6 from v1 remain the same. New step:
//   7. In the Inspector, drag the TextMeshPro component (child of OrientationPrompt)
//      into the new "Prompt Text" slot. Without this the count won't update at runtime.
//
// DEPENDENCIES:
//   ConstellationManager.cs v3.1 — exposes ZoneClusterCount property

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a first-launch orientation prompt with a dynamic planet count.
/// Dismisses after 180° head rotation. Stored in PlayerPrefs — never repeats.
/// </summary>
public class OrientationHelper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root GameObject of the orientation prompt UI. Must have a CanvasGroup.")]
    [SerializeField] private GameObject _promptRoot;

    [Tooltip("TextMeshPro component inside the prompt. Text is updated dynamically at runtime. " +
             "Drag the 3D TextMeshPro child of OrientationPrompt here.")]
    [SerializeField] private TextMeshPro _promptText;

    [Header("Settings")]
    [Tooltip("Degrees of head rotation required to dismiss the prompt")]
    [SerializeField] private float _dismissAngle = 180f;

    [Tooltip("Seconds to fade out when dismissed")]
    [SerializeField] private float _fadeDuration = 1.5f;

    // PlayerPrefs key — do not change after shipping (changing breaks first-launch detection)
    private const string PREFS_KEY = "OrientationShown";

    // ── Private ───────────────────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private float       _startYaw;
    private float       _maxDeltaYaw;
    private bool        _active;
    private bool        _dismissing;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    IEnumerator Start()
    {
        // Already shown on this device — disable immediately
        if (PlayerPrefs.GetInt(PREFS_KEY, 0) == 1)
        {
            if (_promptRoot != null)
                _promptRoot.SetActive(false);
            enabled = false;
            yield break;
        }

        if (_promptRoot == null)
        {
            Debug.LogWarning("[OrientationHelper] promptRoot not assigned. Helper disabled.");
            enabled = false;
            yield break;
        }

        _canvasGroup = _promptRoot.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[OrientationHelper] CanvasGroup missing from promptRoot. Add one.");
            enabled = false;
            yield break;
        }

        // Wait one frame for ConstellationManager.BuildConstellation() to complete
        // so ZoneClusterCount is populated before we set the text
        yield return null;

        // Set dynamic prompt text
        UpdatePromptText();

        _canvasGroup.alpha = 1f;
        _promptRoot.SetActive(true);
        _startYaw    = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
        _maxDeltaYaw = 0f;
        _active      = true;
    }

    void Update()
    {
        if (!_active || _dismissing) return;
        if (Camera.main == null) return;

        float currentYaw = Camera.main.transform.eulerAngles.y;
        float delta      = Mathf.Abs(Mathf.DeltaAngle(_startYaw, currentYaw));

        if (delta > _maxDeltaYaw)
            _maxDeltaYaw = delta;

        if (_maxDeltaYaw >= _dismissAngle)
        {
            _dismissing = true;
            StartCoroutine(FadeAndDismiss());
        }
    }

    // ── Dynamic text ──────────────────────────────────────────────────────────

    private void UpdatePromptText()
    {
        if (_promptText == null) return;

        int planetCount = 0;
        if (ConstellationManager.Instance != null)
            planetCount = ConstellationManager.Instance.ZoneClusterCount;

        _promptText.text = planetCount > 0
            ? $"{planetCount} planets are waiting — look around to find them all."
            : "Your planets are waiting — look around to find them all.";
    }

    // ── Fade and dismiss ──────────────────────────────────────────────────────

    private IEnumerator FadeAndDismiss()
    {
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed           += Time.deltaTime;
            _canvasGroup.alpha  = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _promptRoot.SetActive(false);

        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();

        _active = false;
        Debug.Log("[OrientationHelper] Prompt dismissed and will not show again.");
    }

    // ── Editor helper ─────────────────────────────────────────────────────────

    [ContextMenu("Reset Orientation Shown (Dev)")]
    public void ResetOrientationShown()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        Debug.Log("[OrientationHelper] OrientationShown flag cleared. Restart play to test.");
    }
}
