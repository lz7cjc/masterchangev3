// CrossoverConnector.cs
// Assets/MCAssets/Migration/Scripts/CrossoverConnector.cs
//
// VERSION:  2.0
// TIMESTAMP: 2026-03-09T00:00:00Z
//
// CHANGE LOG:
//   v2.0  2026-03-09  S4.3 swap — MockUserProgress.Instance references replaced
//                     with UserProgressService.Instance. Requires/comment updated.
//   v1.0  2026-03-07  Initial creation — Sprint 3 (S3.5). Two pairs: Flying-Heights,
//                     Water-Sharks.
//
// OBSOLETE: CrossoverConnector.cs v1.0
//
// Purpose   : Draws thin LineRenderer lines between paired crossover zones.
//             A connector is hidden until the user has at least one completion in
//             BOTH zones of the pair.
//
//             Crossover pairs:
//               Flying  ↔  Heights
//               Water   ↔  Sharks
//
//             Each pair is wired via Inspector slots — no hardcoded positions.
//             RefreshVisibility() is called by ConstellationManager.RefreshAllOrbs()
//             so connectors update whenever progress changes.
//
// Requires  : UserProgressService (S4+). ConstellationManager must assign cluster root Transforms.
// ─────────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;

/// <summary>
/// Manages LineRenderer connectors between crossover zone pairs.
/// Attach to an empty GameObject called CrossoverConnectors, child of ConstellationManager.
/// </summary>
public class CrossoverConnector : MonoBehaviour
{
    [System.Serializable]
    public class ConnectorPair
    {
        [Tooltip("Display name for debugging")]
        public string pairName;

        [Tooltip("Transform of the first zone cluster root")]
        public Transform zoneARoot;

        [Tooltip("Transform of the second zone cluster root")]
        public Transform zoneBRoot;

        [Tooltip("PhobiaZone enum value for zone A")]
        public PhobiaZone zoneA;

        [Tooltip("PhobiaZone enum value for zone B")]
        public PhobiaZone zoneB;

        [Tooltip("Colour of the line (use zone A colour at half opacity)")]
        public Color lineColour = new Color(1f, 1f, 1f, 0.3f);

        [HideInInspector] public LineRenderer lineRenderer;
        [HideInInspector] public float currentAlpha;
    }

    [Header("Connector Pairs")]
    [Tooltip("Flying-Heights and Water-Sharks. Add more pairs here as needed.")]
    public ConnectorPair[] pairs;

    [Header("Line Settings")]
    [Tooltip("World-space width of the connector line")]
    [SerializeField] private float _lineWidth = 0.02f;

    [Tooltip("Target alpha when connector is visible")]
    [SerializeField] private float _visibleAlpha = 0.6f;

    [Tooltip("Seconds to fade in when both zones are completed")]
    [SerializeField] private float _fadeDuration = 2.0f;

    [Header("Line Material")]
    [Tooltip("Unlit material for the LineRenderer — assign in Inspector")]
    [SerializeField] private Material _lineMaterial;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        foreach (var pair in pairs)
            InitialisePair(pair);

        RefreshVisibility();
    }

    private void InitialisePair(ConnectorPair pair)
    {
        if (pair.zoneARoot == null || pair.zoneBRoot == null)
        {
            Debug.LogWarning($"[CrossoverConnector] Pair '{pair.pairName}': one or both roots unassigned. Skipping.");
            return;
        }

        GameObject go = new GameObject($"Connector_{pair.pairName}");
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, pair.zoneARoot.position);
        lr.SetPosition(1, pair.zoneBRoot.position);
        lr.startWidth = _lineWidth;
        lr.endWidth = _lineWidth;
        lr.useWorldSpace = true;

        if (_lineMaterial != null)
            lr.material = _lineMaterial;

        Color c = pair.lineColour;
        c.a = 0f;
        lr.startColor = c;
        lr.endColor = c;

        pair.lineRenderer = lr;
        pair.currentAlpha = 0f;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ConstellationManager.RefreshAllOrbs() after any progress change.
    /// Also safe to call every frame — only starts coroutines when state changes.
    /// </summary>
    public void RefreshVisibility()
    {
        foreach (var pair in pairs)
        {
            bool shouldBeVisible = BothZonesHaveCompletion(pair.zoneA, pair.zoneB);
            float targetAlpha = shouldBeVisible ? _visibleAlpha : 0f;

            if (!Mathf.Approximately(pair.currentAlpha, targetAlpha))
                StartCoroutine(FadePair(pair, targetAlpha));
        }
    }

    // ── Progress check ────────────────────────────────────────────────────────

    private bool BothZonesHaveCompletion(PhobiaZone zoneA, PhobiaZone zoneB)
    {
        // S4+: UserProgressService replaces MockUserProgress
        if (UserProgressService.Instance == null || SessionRegistry.Instance == null)
            return false;

        return HasAnyCompletion(zoneA) && HasAnyCompletion(zoneB);
    }

    private bool HasAnyCompletion(PhobiaZone zone)
    {
        foreach (var session in SessionRegistry.Instance.GetByPhobiaZone(zone))
        {
            if (UserProgressService.Instance.IsCompleted(session.sessionID))
                return true;
        }
        return false;
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    private IEnumerator FadePair(ConnectorPair pair, float targetAlpha)
    {
        if (pair.lineRenderer == null) yield break;

        float startAlpha = pair.currentAlpha;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;

            pair.currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            Color c = pair.lineColour;
            c.a = pair.currentAlpha;
            pair.lineRenderer.startColor = c;
            pair.lineRenderer.endColor = c;

            yield return null;
        }

        pair.currentAlpha = targetAlpha;
        Color final = pair.lineColour;
        final.a = targetAlpha;
        pair.lineRenderer.startColor = final;
        pair.lineRenderer.endColor = final;
    }
}