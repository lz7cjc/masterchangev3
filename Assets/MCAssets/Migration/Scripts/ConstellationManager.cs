// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:   3.6
// DATE:      2026-03-22
// TIMESTAMP: 2026-03-22T12:00:00Z
//
// CHANGE LOG:
//   v3.6  2026-03-22  SESSION ORB LABEL COROUTINE FIX
//     - SpawnLabel() gains parentIsActive bool parameter.
//     - Zone planet labels (always active): call label.Show() — coroutine fade safe.
//     - Session orb labels (spawned inactive): call label.SetVisibleImmediate(true)
//       instead of Show(). Avoids "Coroutine couldn't be started because the
//       game object is inactive" errors. Label alpha is correct when the orb
//       is made active by ExpandZone().
//     - No change to zone planet label behaviour.
//
//   v3.5  2026-03-22  Orb sizing as % of planet + session orb labels.
//     - orbSizeAsPercentOfPlanet (Inspector float, default 25f):
//         Session orbs spawn at this percentage of the zone planet's
//         lossy world scale. Applied immediately after Instantiate().
//         Fine-tune in Inspector — no code change needed.
//     - Session orbs now receive a ZoneLabelController label showing
//         session.displayTitle (falls back to session.sessionID if empty).
//         Label uses the existing labelPrefab and labelOffset Inspector slots.
//         Positioned labelOffset units above each session orb, same as
//         zone planet labels.
//     - labelPrefab / labelOffset / zoneConfig Inspector slots retained
//         unchanged — no new Inspector fields required for labels.
//     - Debug logs added for orb scale applied and label spawned per orb.
//
//   v3.4  2026-03-19  Two-tier spawn — zone planets + hidden session orbs.
//   v3.3  2026-03-18  Four missing public methods added as compiler stubs.
//   v3.2  2026-03-15  Instance singleton added.
//   v3.1  2026-03-15  Merged — UserProgressService, ZoneClusterCount, FaceStartZone,
//                     OnSessionSelected wired to SessionLauncher.
//   v3.0  2026-03-15  MockUserProgress → UserProgressService.
//   v2.0  2026-03-09  Zone list refactored to Inspector-driven List<ZoneClusterEntry>.
//   v1    (unversioned) 5 hardcoded zones.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationManager.cs v3.5 (2026-03-22)
//   ConstellationManager.cs v3.4 (2026-03-19)
//   ConstellationManager.cs v3.3 (2026-03-18)
//   ConstellationManager.cs v3.2 (2026-03-15)
//   ConstellationManager.cs v3.1 (2026-03-15)
//   ConstellationManager.cs v3.0 (2026-03-15)
//   ConstellationManager.cs v2.0 (2026-03-09)
//   ConstellationManager.cs v1   (unversioned)
//
// DEPENDENCIES:
//   SessionRegistry.cs            — GetByPhobiaZone(), GetCrossovers()
//   UserProgressService.cs v2.2.0 — IsCompleted()
//   ZonePlanet.cs v1.0            — zone planet behaviour (gaze → expand/collapse)
//   ConstellationOrb.cs v4.0+     — session orb behaviour (gaze → dwell → launch)
//   ZoneLabelController.cs        — SetLabel(), Show() — used for both zone and orb labels
//   OrbVisuals.cs v1.0            — shared emission helpers
//   SessionLauncher.cs            — LaunchSession()
//   PhobiaPriorityManager.cs v1.1 — GetStartZone() (optional — graceful null)
//   SessionData.cs                — PhobiaZone enum, displayTitle field
//
// INSPECTOR SETUP:
//   Prefabs:
//     Zone Planet Prefab — FBX planet prefab with ZonePlanet.cs + GazeHoverTrigger.
//                          One per zone. Always visible in the constellation.
//     Orb Prefab         — Session orb prefab with ConstellationOrb.cs + GazeHoverTrigger.
//                          Spawned hidden as children of the zone planet.
//     Label Prefab       — ZoneLabel prefab with ZoneLabelController + CanvasGroup + TMP.
//                          Used for BOTH zone planet labels AND session orb labels.
//   Zone Labels:
//     Zone Config        — ZoneConfig asset for zone display names (optional — enum fallback).
//     Label Offset       — Vertical offset above centre for label placement (default 0.3).
//   Orb Sizing:
//     Orb Size As Percent Of Planet — session orb scale as % of planet world scale (default 25).
//   Zone Cluster Entries — one entry per active arc zone. No code change when adding a zone.
//   Crossover Connectors — LineRenderer child objects for Flying↔Heights and Water↔Sharks.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ZoneClusterEntry
{
    [Tooltip("The PhobiaZone enum value this cluster root belongs to")]
    public PhobiaZone zone;

    [Tooltip("Empty GameObject that defines the centre of this zone's cluster")]
    public Transform clusterRoot;
}

/// <summary>
/// ConstellationManager v3.5
///
/// Manages the full two-tier constellation:
///   Tier 1 — Zone planets (ZonePlanet.cs): one per zone, always visible.
///             Gaze → dwell → expands session orbs for that zone.
///   Tier 2 — Session orbs (ConstellationOrb.cs): one per SessionData per zone.
///             Spawn hidden. Visible only when parent zone planet is expanded.
///             Gaze → dwell → antechamber → video.
///             Each orb carries a ZoneLabelController label (displayTitle / sessionID).
///
/// Zone list is fully Inspector-driven — no code change needed when adding a zone.
/// </summary>
public class ConstellationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConstellationManager Instance { get; private set; }

    // ── Prefabs ───────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("Zone planet prefab — must have ZonePlanet.cs + GazeHoverTrigger attached. " +
             "One instantiated per zone. Always visible in the constellation.")]
    public GameObject zonePlanetPrefab;

    [Tooltip("Session orb prefab — must have ConstellationOrb.cs + GazeHoverTrigger attached. " +
             "Spawned hidden as children of the zone planet. Shown on zone expand.")]
    public GameObject orbPrefab;

    [Tooltip("Zone label prefab — must have ZoneLabelController.cs, CanvasGroup, and TextMeshPro child. " +
             "Used for zone planet labels AND session orb labels. OPTIONAL — leave empty for no labels.")]
    public GameObject labelPrefab;

    // ── Zone labels ───────────────────────────────────────────────────────────
    [Header("Zone Labels")]
    [Tooltip("ZoneConfig asset — provides display names for zone planet labels. " +
             "If unassigned, zone.ToString() is used as fallback.")]
    public ZoneConfig zoneConfig;

    [Tooltip("Vertical offset above centre where labels appear. " +
             "Applied to both zone planet labels and session orb labels.")]
    [Range(0f, 2f)]
    public float labelOffset = 0.3f;

    // ── Orb sizing ────────────────────────────────────────────────────────────
    [Header("Orb Sizing")]
    [Tooltip("Session orb scale as a percentage of the zone planet's world scale. " +
             "e.g. 25 = session orbs spawn at 25% of the planet size. " +
             "Fine-tune here — no code change needed.")]
    [Range(5f, 100f)]
    public float orbSizeAsPercentOfPlanet = 25f;

    // ── Zone cluster roots ────────────────────────────────────────────────────
    [Header("Zone Cluster Entries")]
    [Tooltip("One entry per active arc zone. Mindfulness excluded — triggered contextually. " +
             "Add rows here when adding new zones — no code change needed.")]
    public List<ZoneClusterEntry> zoneClusterEntries = new List<ZoneClusterEntry>();

    // ── Crossover connectors ──────────────────────────────────────────────────
    [Header("Crossover Connector Lines")]
    [Tooltip("LineRenderer connecting Flying ↔ Heights — shown when crossover unlocked")]
    public LineRenderer flyingHeightsConnector;

    [Tooltip("LineRenderer connecting Water ↔ Sharks — shown when crossover unlocked")]
    public LineRenderer waterSharksConnector;

    // ── Spawn settings ────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Spread radius of session orbs within each expanded zone cluster")]
    public float clusterSpread = 1.2f;

    // ── Startup camera ────────────────────────────────────────────────────────
    [Header("Startup Camera")]
    [Tooltip("Seconds to smoothly rotate camera to face the start zone on launch. 0 = instant snap.")]
    public float cameraFaceDuration = 1.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private List<ConstellationOrb>                   _allOrbs           = new List<ConstellationOrb>();
    private Dictionary<PhobiaZone, ZonePlanet>       _allZonePlanets    = new Dictionary<PhobiaZone, ZonePlanet>();
    private Dictionary<PhobiaZone, List<GameObject>> _sessionOrbsByZone = new Dictionary<PhobiaZone, List<GameObject>>();
    private PhobiaZone _expandedZone = PhobiaZone.None;

    // ── ZoneClusterCount — read by OrientationHelper ──────────────────────────
    /// <summary>Number of zone clusters with an assigned clusterRoot.</summary>
    public int ZoneClusterCount
    {
        get
        {
            int count = 0;
            foreach (var entry in zoneClusterEntries)
                if (entry.clusterRoot != null) count++;
            return count;
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        StartCoroutine(BuildConstellation());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private IEnumerator BuildConstellation()
    {
        // Wait one frame for SessionRegistry and UserProgressService to initialise
        yield return null;

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found. Is it in the scene?");
            yield break;
        }

        if (zonePlanetPrefab == null)
            Debug.LogError("[ConstellationManager] Zone Planet Prefab is not assigned. " +
                           "Assign the FBX planet prefab with ZonePlanet.cs in the Inspector.");

        if (orbPrefab == null)
            Debug.LogError("[ConstellationManager] Orb Prefab is not assigned. " +
                           "Assign the session orb prefab with ConstellationOrb.cs in the Inspector.");

        if (labelPrefab == null)
            Debug.LogWarning("[ConstellationManager] Label Prefab is not assigned — " +
                             "zone planet labels and session orb labels will not appear.");

        Debug.Log($"[ConstellationManager] Building constellation. " +
                  $"Zones: {zoneClusterEntries.Count}. " +
                  $"orbSizeAsPercentOfPlanet: {orbSizeAsPercentOfPlanet}%. " +
                  $"labelPrefab: {(labelPrefab != null ? labelPrefab.name : "none")}.");

        foreach (var entry in zoneClusterEntries)
            SpawnZone(entry.zone, entry.clusterRoot);

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built: {_allZonePlanets.Count} zone planets, " +
                  $"{_allOrbs.Count} session orbs across {ZoneClusterCount} zones.");

        FaceStartZone();
    }

    /// <summary>
    /// Spawns one ZonePlanet at the cluster root (always visible),
    /// then spawns all session orbs for that zone as hidden children of the zone planet.
    /// Session orbs are scaled as a percentage of the planet's world scale.
    /// Each session orb receives a ZoneLabelController label showing displayTitle.
    /// </summary>
    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No cluster root for {zone} — skipping.");
            return;
        }

        // ── Tier 1: Zone planet ───────────────────────────────────────────────
        ZonePlanet spawnedZonePlanet = null;

        if (zonePlanetPrefab != null)
        {
            GameObject planetGO = Instantiate(zonePlanetPrefab, clusterRoot.position,
                                              Quaternion.identity, clusterRoot);
            planetGO.name = $"ZonePlanet_{zone}";

            ZonePlanet zp = planetGO.GetComponent<ZonePlanet>();
            if (zp != null)
            {
                zp.zone = zone;
                _allZonePlanets[zone] = zp;
                spawnedZonePlanet     = zp;
                Debug.Log($"[ConstellationManager] Zone planet spawned for {zone}. " +
                          $"World scale: {planetGO.transform.lossyScale}.");
            }
            else
            {
                Debug.LogWarning($"[ConstellationManager] Zone Planet Prefab has no ZonePlanet component — {zone}.");
            }

            // ── Zone planet label — planet is active, coroutine fade is safe ─
            SpawnLabel(planetGO.transform,
                       zoneConfig != null ? zoneConfig.GetDisplayName(zone) : zone.ToString(),
                       $"Label_{zone}",
                       parentIsActive: true);
        }

        // ── Tier 2: Session orbs (hidden children of the zone planet) ─────────
        if (orbPrefab == null)
        {
            _sessionOrbsByZone[zone] = new List<GameObject>();
            return;
        }

        // Derive orb scale from planet world scale and Inspector percentage
        float orbScale = 0f;
        if (spawnedZonePlanet != null)
        {
            float planetWorldScale = spawnedZonePlanet.transform.lossyScale.x;
            orbScale = planetWorldScale * (orbSizeAsPercentOfPlanet / 100f);
            Debug.Log($"[ConstellationManager] {zone} — planet world scale: {planetWorldScale:F4}, " +
                      $"orb target scale: {orbScale:F4} ({orbSizeAsPercentOfPlanet}%).");
        }
        else
        {
            // No planet was spawned — fall back to a reasonable fixed scale
            orbScale = orbSizeAsPercentOfPlanet / 100f;
            Debug.LogWarning($"[ConstellationManager] {zone} — no zone planet to derive scale from. " +
                             $"Using fallback orb scale: {orbScale:F4}.");
        }

        List<SessionData> sessions = SessionRegistry.Instance.GetByPhobiaZone(zone);
        List<GameObject>  orbGOs   = new List<GameObject>();

        Transform orbParent = spawnedZonePlanet != null
            ? spawnedZonePlanet.transform
            : clusterRoot;

        for (int i = 0; i < sessions.Count; i++)
        {
            SessionData session = sessions[i];
            Vector3     localPos = AutoPosition(i, sessions.Count, session.level);

            GameObject orbGO = Instantiate(orbPrefab,
                                           orbParent.position + localPos,
                                           Quaternion.identity,
                                           orbParent);
            orbGO.name = session.sessionID;

            // Apply size as % of planet
            orbGO.transform.localScale = Vector3.one * orbScale;

            // Start hidden — shown only when zone planet is expanded
            orbGO.SetActive(false);

            ConstellationOrb orb = orbGO.GetComponent<ConstellationOrb>();
            if (orb != null)
            {
                orb.session = session;
                orb.onSessionSelected.AddListener(OnSessionSelected);
                orb.RefreshState();
                _allOrbs.Add(orb);
            }

            // ── Session orb label — displayTitle, fallback to sessionID ───────
            string labelText = !string.IsNullOrEmpty(session.displayTitle)
                ? session.displayTitle
                : session.sessionID;

            // ── Session orb label — orb is inactive at spawn, use SetVisibleImmediate ─
            SpawnLabel(orbGO.transform, labelText, $"Label_{session.sessionID}",
                       parentIsActive: false);

            Debug.Log($"[ConstellationManager] Orb spawned: '{session.sessionID}' " +
                      $"zone={zone} level={session.level} scale={orbScale:F4} " +
                      $"label='{labelText}'.");

            orbGOs.Add(orbGO);
        }

        _sessionOrbsByZone[zone] = orbGOs;

        Debug.Log($"[ConstellationManager] SpawnZone complete: {zone} — " +
                  $"{orbGOs.Count} session orbs.");
    }

    /// <summary>
    /// Instantiates the label prefab as a child of parent, sets its text, and makes it visible.
    /// parentIsActive: true for zone planets (always active — coroutine fade is safe).
    ///                 false for session orbs (spawned inactive — coroutine would throw;
    ///                 SetVisibleImmediate sets alpha directly so it is visible when the orb activates).
    /// No-op if labelPrefab is null.
    /// </summary>
    private void SpawnLabel(Transform parent, string text, string goName, bool parentIsActive)
    {
        if (labelPrefab == null) return;

        GameObject labelGO = Instantiate(labelPrefab,
                                         parent.position + Vector3.up * labelOffset,
                                         Quaternion.identity,
                                         parent);
        labelGO.name = goName;

        ZoneLabelController label = labelGO.GetComponent<ZoneLabelController>();
        if (label != null)
        {
            label.SetLabel(text);

            if (parentIsActive)
                label.Show();                    // coroutine fade — safe on active object
            else
                label.SetVisibleImmediate(true); // direct alpha set — safe on inactive object

            Debug.Log($"[ConstellationManager] Label spawned: '{goName}' text='{text}' " +
                      $"parentIsActive={parentIsActive}.");
        }
        else
        {
            Debug.LogWarning($"[ConstellationManager] labelPrefab is missing ZoneLabelController " +
                             $"on '{labelGO.name}'.");
        }
    }

    /// <summary>
    /// Positions session orbs within an expanded zone cluster.
    /// L3 = eye level. Lower levels descend, higher levels rise.
    /// </summary>
    private Vector3 AutoPosition(int index, int total, int level)
    {
        float y     = (level - 3) * 0.6f;
        float angle = (index / (float)Mathf.Max(total - 1, 1)) * 120f - 60f;
        float rad   = angle * Mathf.Deg2Rad;
        float x     = Mathf.Sin(rad) * clusterSpread;
        float z     = Mathf.Cos(rad) * clusterSpread * 0.5f;
        return new Vector3(x, y, z);
    }

    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] Session selected: {session.sessionID}");

        // Route via AntechamberController if present, otherwise direct launch
        if (AntechamberController.Instance != null)
            AntechamberController.Instance.ShowForSession(session);
        else
            SessionLauncher.Instance?.LaunchSession(session);
    }

    // ── Expand / Collapse ─────────────────────────────────────────────────────

    /// <summary>
    /// Called by ZonePlanet when the user dwells on it.
    /// Shows session orbs for this zone, collapses any previously expanded zone.
    /// </summary>
    public void ExpandZone(PhobiaZone zone)
    {
        // Collapse the previously expanded zone first (mutual exclusivity)
        if (_expandedZone != PhobiaZone.None && _expandedZone != zone)
            CollapseZoneInternal(_expandedZone, notifyZonePlanet: true);

        _expandedZone = zone;

        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbs))
        {
            foreach (var orb in orbs)
                if (orb != null) orb.SetActive(true);

            Debug.Log($"[ConstellationManager] Expanded zone: {zone} ({orbs.Count} session orbs shown).");
        }
        else
        {
            Debug.LogWarning($"[ConstellationManager] ExpandZone: no session orbs found for {zone}.");
        }
    }

    /// <summary>
    /// Called by ZonePlanet when the user dwells on an already-expanded zone to collapse it,
    /// or when another zone is expanded (forcing collapse without a gaze interaction).
    /// </summary>
    public void CollapseZone(PhobiaZone zone)
    {
        CollapseZoneInternal(zone, notifyZonePlanet: false);
    }

    private void CollapseZoneInternal(PhobiaZone zone, bool notifyZonePlanet)
    {
        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbs))
        {
            foreach (var orb in orbs)
                if (orb != null) orb.SetActive(false);
        }

        if (notifyZonePlanet && _allZonePlanets.TryGetValue(zone, out ZonePlanet zp))
            zp.Collapse();

        if (_expandedZone == zone)
            _expandedZone = PhobiaZone.None;

        Debug.Log($"[ConstellationManager] Collapsed zone: {zone}.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call after any progress change to refresh all session orb visuals and connectors.</summary>
    public void RefreshAllOrbs()
    {
        foreach (var orb in _allOrbs)
            orb.RefreshState();
        UpdateCrossoverConnectors();
    }

    /// <summary>Stub — scene navigation is post-MVP.</summary>
    public void NavigateToVestibularWithBookmark(SessionData bookmark)
    {
        Debug.Log($"[ConstellationManager] NavigateToVestibularWithBookmark: " +
                  $"bookmark={bookmark?.sessionID ?? "null"} (scene navigation — post-MVP stub)");
    }

    /// <summary>Stub — scene navigation is post-MVP.</summary>
    public void ReturnToConstellationWithZoneExpanded(PhobiaZone zone)
    {
        if (_sessionOrbsByZone.ContainsKey(zone))
            ExpandZone(zone);
        Debug.Log($"[ConstellationManager] ReturnToConstellationWithZoneExpanded: {zone} " +
                  $"(scene navigation — post-MVP stub)");
    }

    // ── Startup camera ────────────────────────────────────────────────────────

    private void FaceStartZone()
    {
        Transform target = null;

        if (PhobiaPriorityManager.Instance != null)
        {
            PhobiaZone startZone = PhobiaPriorityManager.Instance.GetStartZone();
            target = GetClusterRoot(startZone);
        }

        if (target == null)
        {
            foreach (var entry in zoneClusterEntries)
            {
                if (entry.clusterRoot != null) { target = entry.clusterRoot; break; }
            }
        }

        if (target == null) return;

        if (cameraFaceDuration <= 0f)
            SnapCameraToFace(target.position);
        else
            StartCoroutine(SmoothFaceTarget(target.position, cameraFaceDuration));
    }

    private void SnapCameraToFace(Vector3 targetPos)
    {
        if (Camera.main == null) return;
        Vector3 dir = targetPos - Camera.main.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Transform rig = Camera.main.transform.parent ?? Camera.main.transform;
        rig.rotation  = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private IEnumerator SmoothFaceTarget(Vector3 targetPos, float duration)
    {
        if (Camera.main == null) yield break;
        Transform  rig    = Camera.main.transform.parent ?? Camera.main.transform;
        Quaternion startQ = rig.rotation;
        Vector3    dir    = targetPos - Camera.main.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;
        Quaternion endQ = Quaternion.LookRotation(dir.normalized, Vector3.up);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            rig.rotation  = Quaternion.Slerp(startQ, endQ, elapsed / duration);
            yield return null;
        }
        rig.rotation = endQ;
    }

    private Transform GetClusterRoot(PhobiaZone zone)
    {
        foreach (var entry in zoneClusterEntries)
            if (entry.zone == zone && entry.clusterRoot != null)
                return entry.clusterRoot;
        return null;
    }

    // ── Crossover connectors ──────────────────────────────────────────────────

    private void UpdateCrossoverConnectors()
    {
        if (UserProgressService.Instance == null) return;

        if (flyingHeightsConnector != null)
        {
            bool unlocked = SessionRegistry.Instance.GetCrossovers(PhobiaZone.Heights).Count > 0
                            && HasCompletedZone(PhobiaZone.Heights, 2);
            flyingHeightsConnector.gameObject.SetActive(unlocked);
        }

        if (waterSharksConnector != null)
        {
            bool unlocked = HasCompletedZone(PhobiaZone.Water, 2);
            waterSharksConnector.gameObject.SetActive(unlocked);
        }
    }

    private bool HasCompletedZone(PhobiaZone zone, int minCompletions)
    {
        int count = 0;
        foreach (var s in SessionRegistry.Instance.GetByPhobiaZone(zone))
            if (UserProgressService.Instance.IsCompleted(s.sessionID)) count++;
        return count >= minCompletions;
    }
}
