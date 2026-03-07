// OrientationHelper.cs
// MasterChange VR — Sprint 3 (S3.5)
// Version   : v1
// Created   : 2026-03-07
// Location  : Assets/MCAssets/Migration/Scripts/OrientationHelper.cs
//             (attach to GameManager)
//
// Purpose   : On the very first time a user opens the Constellation scene, displays
//             a gentle world-space prompt ("Look around — your zones are waiting.")
//             positioned 3m ahead at eye level.
//
//             The prompt disappears automatically once the user has rotated their
//             head at least 180° from their starting direction. It never shows again
//             (stored in PlayerPrefs — device-side UI hint only, not Supabase data).
//
// Scene setup:
//   1. Attach this script to GameManager.
//   2. Create an empty child GameObject called OrientationPrompt.
//   3. Add a CanvasGroup component to OrientationPrompt.
//   4. Add a 3D TextMeshPro (3D Object → Text - TextMeshPro) as a child of
//      OrientationPrompt. Text: "Look around — your zones are waiting."
//      Font size 0.4, Bold, White, Centre-aligned.
//   5. Position OrientationPrompt at (0, 0, 3) relative to GameManager
//      (3m ahead of the player spawn point).
//   6. Assign the OrientationPrompt GameObject to the promptRoot slot below.
//
// Change log:
//   v1  2026-03-07  Initial creation.
// ─────────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;

/// <summary>
/// Displays a first-launch orientation prompt. Dismisses after 180° head rotation.
/// Stored in PlayerPrefs ("OrientationShown") — never repeats.
/// </summary>
public class OrientationHelper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root GameObject of the orientation prompt UI. Must have a CanvasGroup.")]
    [SerializeField] private GameObject _promptRoot;

    [Header("Settings")]
    [Tooltip("Degrees of head rotation required to dismiss the prompt")]
    [SerializeField] private float _dismissAngle = 180f;

    [Tooltip("Seconds to fade out when dismissed")]
    [SerializeField] private float _fadeDuration = 1.5f;

    // PlayerPrefs key — do not change after shipping
    private const string PREFS_KEY = "OrientationShown";

    // ── Private ───────────────────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private float       _startYaw;
    private float       _maxDeltaYaw;
    private bool        _active;
    private bool        _dismissing;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Already shown on this device — disable immediately, no fade needed
        if (PlayerPrefs.GetInt(PREFS_KEY, 0) == 1)
        {
            if (_promptRoot != null)
                _promptRoot.SetActive(false);
            enabled = false;
            return;
        }

        if (_promptRoot == null)
        {
            Debug.LogWarning("[OrientationHelper] promptRoot not assigned. Helper disabled.");
            enabled = false;
            return;
        }

        _canvasGroup = _promptRoot.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[OrientationHelper] CanvasGroup missing from promptRoot. Add one.");
            enabled = false;
            return;
        }

        _canvasGroup.alpha = 1f;
        _promptRoot.SetActive(true);

        // Record starting yaw from main camera
        _startYaw    = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
        _maxDeltaYaw = 0f;
        _active      = true;
    }

    void Update()
    {
        if (!_active || _dismissing) return;

        if (Camera.main == null) return;

        float currentYaw = Camera.main.transform.eulerAngles.y;
        float delta = Mathf.Abs(Mathf.DeltaAngle(_startYaw, currentYaw));

        if (delta > _maxDeltaYaw)
            _maxDeltaYaw = delta;

        if (_maxDeltaYaw >= _dismissAngle)
        {
            _dismissing = true;
            StartCoroutine(FadeAndDismiss());
        }
    }

    // ── Fade and dismiss ──────────────────────────────────────────────────────

    private IEnumerator FadeAndDismiss()
    {
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed            += Time.deltaTime;
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

    /// <summary>
    /// Resets the PlayerPrefs flag so the prompt shows again on next play.
    /// Use during development to test first-launch behaviour.
    /// </summary>
    [ContextMenu("Reset Orientation Shown (Dev)")]
    public void ResetOrientationShown()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        Debug.Log("[OrientationHelper] OrientationShown flag cleared. Restart play to test.");
    }
}
