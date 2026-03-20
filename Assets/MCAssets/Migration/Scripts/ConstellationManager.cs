// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:  3.5
// DATE:     2026-03-20
// TIMESTAMP: 2026-03-20T00:00:00Z
//
// CHANGE LOG:
//   v3.5  2026-03-20  SCENE-PLACED PLANETS — ORB PREFAB OPTIONAL
//     - zonePlanetPrefab slot REMOVED. Planets are no longer instantiated from a
//       prefab. Each zone's planet must already exist in the scene as a child of
//       its cluster root with ZonePlanet.cs attached. SpawnZone() now finds it
//       via GetComponentInChildren<ZonePlanet>() on the cluster root.
//     - orbPrefab is now fully optional. Null = no session orbs for any zone —
//       no error, no warning. Zones with no sessions registered also produce no
//       orbs silently.
//     - BuildConstellation() null-guards for zonePlanetPrefab and orbPrefab
//       REMOVED — they were the source of the startup errors.
//     - SpawnZone() logs a clear Debug.Log (not error) when no planet is found
//       under a cluster root, to assist setup without spamming errors.
//     - All other behaviour (expand/collapse, crossovers, camera face) unchanged.
//
//   v3.4  2026-03-19  TWO-TIER SPAWN — FULL IMPLEMENTATION.
//   v3.3  2026-03-18  Four missing public methods added as compiler stubs.
//   v3.2  2026-03-15  Instance singleton added.
//   v3.1  2026-03-15  MERGED — UserProgressService, ZoneClusterCount, FaceStartZone,
//                     OnSessionSelected wired to SessionLauncher.
//   v3.0  2026-03-15  MockUserProgress → UserProgressService.
//   v2.0  2026-03-09  Zone list refactored to Inspector-driven List<ZoneClusterEntry>.
//   v1    (unversioned) 5 hardcoded zones.
//
// OBSOLETE FILES — DELETE THESE:
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
//   OrbVisuals.cs v1.0            — shared emission helpers
//   SessionLauncher.cs            — LaunchSession()
//   PhobiaPriorityManager.cs v1.1 — GetStartZone() (optional — graceful null)
//   SessionData.cs                — PhobiaZone enum
//
// INSPECTOR SETUP:
//   Orb Prefab (optional)    — Session orb prefab with ConstellationOrb.cs +
//                              GazeHoverTrigger. Leave empty if no session orbs
//                              are used yet. No error will be raised.
//   Zone Cluster Entries     — One entry per active arc zone (Mindfulness excluded).
//                              Each cluster root must already have a child GameObject
//                              with ZonePlanet.cs attached — this is the scene-placed
//                              planet. No prefab instantiation occurs.
//   Crossover Connectors     — LineRenderer child objects on ConstellationManager.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ZoneClusterEntry
{
    [Tooltip("The PhobiaZone enum value this cluster root belongs to")]
    public PhobiaZone zone;

    [Tooltip("Empty GameObject that defines the centre of this zone's cluster. " +
             "Must have a child with ZonePlanet.cs attached.")]
    public Transform clusterRoot;
}

/// <summary>
/// ConstellationManager v3.5
///
/// Manages the full two-tier constellation:
///   Tier 1 — Zone planets (ZonePlanet.cs): one per zone, always visible.
///             Must be placed in the scene as children of each cluster root.
///             Gaze → dwell → expands session orbs for that zone.
///   Tier 2 — Session orbs (ConstellationOrb.cs): one per SessionData per zone.
///             Spawned from orbPrefab at runtime. Optional — if orbPrefab is null,
///             no orbs are spawned and no error is raised.
///             Gaze → dwell → antechamber (if present) → video.
///
/// Zone list is fully Inspector-driven — no code change needed when adding a zone.
/// </summary>
public class ConstellationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConstellationManager Instance { get; private set; }

    // ── Prefabs ───────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("Session orb prefab — must have ConstellationOrb.cs + GazeHoverTrigger attached. " +
             "Spawned hidden as children of the zone planet on zone expand. " +
             "OPTIONAL — leave empty if session orbs are not yet in use.")]
    public GameObject orbPrefab;

    // ── Zone cluster roots ────────────────────────────────────────────────────
    [Header("Zone Cluster Entries")]
    [Tooltip("One entry per active arc zone. Mindfulness excluded — triggered contextually. " +
             "Each cluster root must contain a child GameObject with ZonePlanet.cs. " +
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
        Debug.Log("[ConstellationManager] Start — singleton set, beginning BuildConstellation coroutine.");
        StartCoroutine(BuildConstellation());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private IEnumerator BuildConstellation()
    {
        // Wait one frame for SessionRegistry and UserProgressService to initialise
        yield return null;

        Debug.Log("[ConstellationManager] BuildConstellation — frame delay complete, proceeding.");

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found. Is it in the scene?");
            yield break;
        }

        Debug.Log($"[ConstellationManager] SessionRegistry found. " +
                  $"orbPrefab={(orbPrefab != null ? orbPrefab.name : "none (optional)")}. " +
                  $"Zone cluster entries: {zoneClusterEntries.Count}.");

        foreach (var entry in zoneClusterEntries)
        {
            Debug.Log($"[ConstellationManager] Processing zone: {entry.zone}, " +
                      $"clusterRoot={(entry.clusterRoot != null ? entry.clusterRoot.name : "NULL")}.");
            SpawnZone(entry.zone, entry.clusterRoot);
        }

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built: {_allZonePlanets.Count} zone planets, " +
                  $"{_allOrbs.Count} session orbs across {ZoneClusterCount} zones.");

        FaceStartZone();
    }

    /// <summary>
    /// Registers the scene-placed ZonePlanet found under the cluster root,
    /// then optionally spawns session orbs as hidden children if orbPrefab is assigned.
    /// </summary>
    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] SpawnZone: no cluster root assigned for {zone} — skipping.");
            return;
        }

        // ── Tier 1: Find scene-placed planet ─────────────────────────────────
        ZonePlanet zp = clusterRoot.GetComponentInChildren<ZonePlanet>();

        if (zp != null)
        {
            zp.zone = zone;
            _allZonePlanets[zone] = zp;
            Debug.Log($"[ConstellationManager] SpawnZone: found ZonePlanet '{zp.gameObject.name}' " +
                      $"under '{clusterRoot.name}' for zone {zone}.");
        }
        else
        {
            Debug.Log($"[ConstellationManager] SpawnZone: no ZonePlanet found under '{clusterRoot.name}' " +
                      $"for zone {zone}. Planet must be a child of the cluster root with ZonePlanet.cs attached.");
        }

        // ── Tier 2: Session orbs (optional) ──────────────────────────────────
        if (orbPrefab == null)
        {
            Debug.Log($"[ConstellationManager] SpawnZone: orbPrefab not assigned — " +
                      $"skipping session orb spawn for {zone} (optional, no error).");
            _sessionOrbsByZone[zone] = new List<GameObject>();
            return;
        }

        List<SessionData> sessions = SessionRegistry.Instance.GetByPhobiaZone(zone);

        if (sessions == null || sessions.Count == 0)
        {
            Debug.Log($"[ConstellationManager] SpawnZone: no sessions registered for {zone} — " +
                      $"no orbs spawned.");
            _sessionOrbsByZone[zone] = new List<GameObject>();
            return;
        }

        // Parent orbs to the zone planet if found, otherwise to cluster root
        Transform orbParent = zp != null ? zp.transform : clusterRoot;

        List<GameObject> orbGOs = new List<GameObject>();

        for (int i = 0; i < sessions.Count; i++)
        {
            SessionData session = sessions[i];
            Vector3 localPos    = AutoPosition(i, sessions.Count, session.level);

            GameObject orbGO = Instantiate(orbPrefab,
                                           orbParent.position + localPos,
                                           Quaternion.identity,
                                           orbParent);
            orbGO.name = session.sessionID;
            orbGO.SetActive(false);  // Hidden until zone planet is expanded

            ConstellationOrb orb = orbGO.GetComponent<ConstellationOrb>();
            if (orb != null)
            {
                orb.session = session;
                orb.onSessionSelected.AddListener(OnSessionSelected);
                orb.RefreshState();
                _allOrbs.Add(orb);
                Debug.Log($"[ConstellationManager] SpawnZone: spawned orb '{orbGO.name}' " +
                          $"for zone {zone}, level {session.level}.");
            }
            else
            {
                Debug.LogWarning($"[ConstellationManager] SpawnZone: orb '{orbGO.name}' " +
                                 $"is missing ConstellationOrb component.");
            }

            orbGOs.Add(orbGO);
        }

        _sessionOrbsByZone[zone] = orbGOs;
        Debug.Log($"[ConstellationManager] SpawnZone: {orbGOs.Count} orbs spawned for {zone}.");
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
        Debug.Log($"[ConstellationManager] ExpandZone called for {zone}. " +
                  $"Previously expanded: {_expandedZone}.");

        // Collapse the previously expanded zone first (mutual exclusivity)
        if (_expandedZone != PhobiaZone.None && _expandedZone != zone)
        {
            CollapseZoneInternal(_expandedZone, notifyZonePlanet: true);
        }

        _expandedZone = zone;

        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbs))
        {
            int shown = 0;
            foreach (var orb in orbs)
            {
                if (orb != null)
                {
                    orb.SetActive(true);
                    shown++;
                }
            }
            Debug.Log($"[ConstellationManager] ExpandZone: {zone} — {shown} orbs shown.");
        }
        else
        {
            Debug.Log($"[ConstellationManager] ExpandZone: {zone} — no orbs registered " +
                      $"(orbPrefab not assigned or no sessions for this zone).");
        }
    }

    /// <summary>
    /// Called by ZonePlanet when the user dwells on an already-expanded zone to collapse it,
    /// or when another zone is expanded (forcing collapse without a gaze interaction).
    /// </summary>
    public void CollapseZone(PhobiaZone zone)
    {
        Debug.Log($"[ConstellationManager] CollapseZone called for {zone}.");
        CollapseZoneInternal(zone, notifyZonePlanet: false);
    }

    private void CollapseZoneInternal(PhobiaZone zone, bool notifyZonePlanet)
    {
        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbs))
        {
            int hidden = 0;
            foreach (var orb in orbs)
            {
                if (orb != null)
                {
                    orb.SetActive(false);
                    hidden++;
                }
            }
            Debug.Log($"[ConstellationManager] CollapseZoneInternal: {zone} — {hidden} orbs hidden.");
        }

        if (notifyZonePlanet && _allZonePlanets.TryGetValue(zone, out ZonePlanet zp))
        {
            zp.Collapse();
            Debug.Log($"[ConstellationManager] CollapseZoneInternal: notified ZonePlanet for {zone}.");
        }

        if (_expandedZone == zone)
            _expandedZone = PhobiaZone.None;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call after any progress change to refresh all session orb visuals and connectors.</summary>
    public void RefreshAllOrbs()
    {
        Debug.Log($"[ConstellationManager] RefreshAllOrbs — refreshing {_allOrbs.Count} orbs.");
        foreach (var orb in _allOrbs)
            orb.RefreshState();
        UpdateCrossoverConnectors();
    }

    /// <summary>
    /// Stub — called by AntechamberController when the user chooses to complete
    /// the Vestibular programme before a session.
    /// Scene navigation is post-MVP.
    /// </summary>
    public void NavigateToVestibularWithBookmark(SessionData bookmark)
    {
        Debug.Log($"[ConstellationManager] NavigateToVestibularWithBookmark: " +
                  $"bookmark={bookmark?.sessionID ?? "null"} (scene navigation — post-MVP stub)");
    }

    /// <summary>
    /// Stub — called by AntechamberController when the user chooses a different session.
    /// Scene navigation is post-MVP. The zone is expanded in-memory but no scene transition occurs.
    /// </summary>
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
            Debug.Log($"[ConstellationManager] FaceStartZone: PhobiaPriorityManager start zone = {startZone}.");
        }

        if (target == null)
        {
            foreach (var entry in zoneClusterEntries)
            {
                if (entry.clusterRoot != null) { target = entry.clusterRoot; break; }
            }
        }

        if (target == null)
        {
            Debug.LogWarning("[ConstellationManager] FaceStartZone: no valid cluster root found — camera not rotated.");
            return;
        }

        Debug.Log($"[ConstellationManager] FaceStartZone: facing '{target.name}', duration={cameraFaceDuration}s.");

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
        Debug.Log("[ConstellationManager] SnapCameraToFace complete.");
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
        Debug.Log("[ConstellationManager] SmoothFaceTarget complete.");
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
        if (UserProgressService.Instance == null)
        {
            Debug.LogWarning("[ConstellationManager] UpdateCrossoverConnectors: UserProgressService not available.");
            return;
        }

        if (flyingHeightsConnector != null)
        {
            bool unlocked = SessionRegistry.Instance.GetCrossovers(PhobiaZone.Heights).Count > 0
                            && HasCompletedZone(PhobiaZone.Heights, 2);
            flyingHeightsConnector.gameObject.SetActive(unlocked);
            Debug.Log($"[ConstellationManager] Crossover Flying↔Heights: unlocked={unlocked}.");
        }

        if (waterSharksConnector != null)
        {
            bool unlocked = HasCompletedZone(PhobiaZone.Water, 2);
            waterSharksConnector.gameObject.SetActive(unlocked);
            Debug.Log($"[ConstellationManager] Crossover Water↔Sharks: unlocked={unlocked}.");
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
