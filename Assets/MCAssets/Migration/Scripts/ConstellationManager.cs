// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:  3.3
// DATE:     2026-03-18
// TIMESTAMP: 2026-03-18T12:00:00Z
//
// CHANGE LOG:
//   v3.3  2026-03-18  Compiler error fixes — four missing public methods added
//                     as MVP stubs (no new functionality introduced):
//                       NavigateToVestibularWithBookmark(SessionData)
//                       ReturnToConstellationWithZoneExpanded(PhobiaZone)
//                       ExpandZone(PhobiaZone)
//                       CollapseZone(PhobiaZone)
//                     Called by AnteChamberController v3.0 and ZonePlanet v1.0.
//                     Full navigation/expand behaviour is post-MVP scope.
//
//   v3.2  2026-03-15  Instance singleton added.
//                     OrientationHelper v2 calls ConstellationManager.Instance.ZoneClusterCount
//                     at runtime — requires a singleton. Instance is set in Start() with
//                     standard duplicate guard. Supersedes v3.1.
//
//   v3.1  2026-03-15  MERGED — three change streams consolidated into one file
//     - MockUserProgress → UserProgressService in UpdateCrossoverConnectors()
//       and HasCompletedZone().
//     - ZoneClusterCount public property added — exposes assigned cluster count
//       at runtime. Required by OrientationHelper v2 for "X planets are waiting"
//       dynamic message. Read-only; computed from zoneClusterEntries.
//     - FaceStartZone() added — rotates camera to face the user's priority start
//       zone after BuildConstellation() completes. Reads
//       PhobiaPriorityManager.Instance.GetStartZone() with graceful null fallback
//       to first assigned cluster root. Smooth rotation (Inspector-tunable duration)
//       or instant snap (set cameraFaceDuration = 0). This is the Chapter D
//       snippet referenced in Setup Guide v7.0.
//     - OnSessionSelected() wired to SessionLauncher.Instance.LaunchSession()
//       (was a stub TODO).
//     - Zone list is a serialised List<ZoneClusterEntry> — no code change
//       needed when adding a new zone (add row in Inspector).
//
//   v3.0  2026-03-15  MockUserProgress → UserProgressService (superseded by v3.1)
//
//   v2.0  2026-03-09  Zone list refactored to List<ZoneClusterEntry>.
//                     All 11 arc zones. Inspector-driven. No hardcoded zone list.
//
//   v1    (unversioned) 5 hardcoded zones — Flying, Heights, Water, Sharks, Crowds.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationManager.cs v3.2 (2026-03-15 — superseded)
//   ConstellationManager.cs v3.1 (2026-03-15 — superseded)
//   ConstellationManager.cs v3.0 (2026-03-15 — superseded same day)
//   ConstellationManager.cs v2.0 (2026-03-09)
//   ConstellationManager.cs v1   (unversioned)
//
// DEPENDENCIES:
//   SessionRegistry.cs          — GetByPhobiaZone(), GetCrossovers()
//   UserProgressService.cs v2.2.0 — IsCompleted()
//   ConstellationOrb.cs  v4.0   — planet mesh, UserProgressService.OrbState
//   SessionLauncher.cs          — LaunchSession()
//   PhobiaPriorityManager.cs v1.1 — GetStartZone() (optional — graceful null)
//   SessionData.cs              — PhobiaZone enum
//
// INSPECTOR SETUP:
//   Zone Cluster Roots — one entry per active arc zone.
//   Mindfulness excluded (triggered contextually, not in arc).
//   Add entries here — no code change needed when adding zones.
//   Crossover Connectors — LineRenderer child objects on ConstellationManager.
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
/// ConstellationManager v3.3 — spawns all session orbs, manages crossover connectors,
/// and rotates the camera to face the user's priority zone on launch.
/// Zone list is fully Inspector-driven via ZoneClusterEntry — no code change
/// needed when adding a new zone.
/// </summary>
public class ConstellationManager : MonoBehaviour
{
    [Header("Orb Prefab")]
    [Tooltip("Prefab with ConstellationOrb v4 attached — supports planet mesh hierarchy")]
    public GameObject orbPrefab;

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConstellationManager Instance { get; private set; }

    [Header("Zone Cluster Roots")]
    [Tooltip("One entry per active arc zone. Mindfulness excluded — triggered contextually, not in arc.")]
    public List<ZoneClusterEntry> zoneClusterEntries = new List<ZoneClusterEntry>();

    [Header("Crossover Connector Lines")]
    [Tooltip("LineRenderer connecting Flying ↔ Heights — shown when crossover unlocked")]
    public LineRenderer flyingHeightsConnector;

    [Tooltip("LineRenderer connecting Water ↔ Sharks — shown when crossover unlocked")]
    public LineRenderer waterSharksConnector;

    [Header("Spawn Settings")]
    [Tooltip("Spread radius of orbs within each cluster")]
    public float clusterSpread = 1.2f;

    [Header("Startup Camera")]
    [Tooltip("Seconds to smoothly rotate camera to face the start zone on launch. 0 = instant snap.")]
    public float cameraFaceDuration = 1.5f;

    // ── Private ───────────────────────────────────────────────────────────────
    private List<ConstellationOrb> _allOrbs = new List<ConstellationOrb>();

    // ── ZoneClusterCount — read by OrientationHelper v2 ───────────────────────
    /// <summary>
    /// Number of zone clusters with an assigned clusterRoot.
    /// OrientationHelper v2 reads this after BuildConstellation() to display
    /// "X planets are waiting — look around to find them all."
    /// </summary>
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

    void Start()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        StartCoroutine(BuildConstellation());
    }

    private IEnumerator BuildConstellation()
    {
        // Wait one frame for SessionRegistry and UserProgressService to initialise
        yield return null;

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found. Is it in the scene?");
            yield break;
        }

        foreach (var entry in zoneClusterEntries)
            SpawnZone(entry.zone, entry.clusterRoot);

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built constellation: {_allOrbs.Count} orbs across " +
                  $"{ZoneClusterCount} zone clusters.");

        // Rotate camera to face the user's priority start zone
        FaceStartZone();
    }

    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No cluster root assigned for {zone} — skipping.");
            return;
        }

        List<SessionData> sessions = SessionRegistry.Instance.GetByPhobiaZone(zone);

        for (int i = 0; i < sessions.Count; i++)
        {
            SessionData session  = sessions[i];
            Vector3 spawnPos     = clusterRoot.position + AutoPosition(i, sessions.Count, session.level);
            GameObject orbGO     = Instantiate(orbPrefab, spawnPos, Quaternion.identity, clusterRoot);
            orbGO.name           = session.sessionID;

            ConstellationOrb orb = orbGO.GetComponent<ConstellationOrb>();
            if (orb != null)
            {
                orb.session = session;
                orb.onSessionSelected.AddListener(OnSessionSelected);
                orb.RefreshState();
                _allOrbs.Add(orb);
            }
        }
    }

    private Vector3 AutoPosition(int index, int total, int level)
    {
        float y     = (level - 3) * 0.6f;   // L3 = eye level; lower levels below, higher above
        float angle = (index / (float)Mathf.Max(total - 1, 1)) * 120f - 60f;
        float rad   = angle * Mathf.Deg2Rad;
        float x     = Mathf.Sin(rad) * clusterSpread;
        float z     = Mathf.Cos(rad) * clusterSpread * 0.5f;
        return new Vector3(x, y, z);
    }

    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] Session selected: {session.displayTitle}");
        SessionLauncher.Instance?.LaunchSession(session);
    }

    // ── Startup camera facing ─────────────────────────────────────────────────

    /// <summary>
    /// Rotates the camera to face the user's priority start zone on launch.
    /// Priority order (via PhobiaPriorityManager.GetStartZone()):
    ///   1. Last visited zone
    ///   2. Highest priority zone with progress (started, not complete)
    ///   3. Top of user's priority list
    ///   4. First assigned cluster root (fallback if PhobiaPriorityManager absent)
    /// </summary>
    private void FaceStartZone()
    {
        Transform targetCluster = null;

        if (PhobiaPriorityManager.Instance != null)
        {
            PhobiaZone startZone = PhobiaPriorityManager.Instance.GetStartZone();
            targetCluster = GetClusterRoot(startZone);
        }

        // Fallback to first assigned cluster root
        if (targetCluster == null)
        {
            foreach (var entry in zoneClusterEntries)
            {
                if (entry.clusterRoot != null)
                {
                    targetCluster = entry.clusterRoot;
                    break;
                }
            }
        }

        if (targetCluster == null) return;

        if (cameraFaceDuration <= 0f)
            SnapCameraToFace(targetCluster.position);
        else
            StartCoroutine(SmoothFaceTarget(targetCluster.position, cameraFaceDuration));
    }

    private void SnapCameraToFace(Vector3 targetWorldPos)
    {
        if (Camera.main == null) return;
        Vector3 dir = (targetWorldPos - Camera.main.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Transform rig = Camera.main.transform.parent ?? Camera.main.transform;
        rig.rotation  = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private IEnumerator SmoothFaceTarget(Vector3 targetWorldPos, float duration)
    {
        if (Camera.main == null) yield break;
        Transform rig     = Camera.main.transform.parent ?? Camera.main.transform;
        Quaternion startQ = rig.rotation;
        Vector3 dir       = targetWorldPos - Camera.main.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;
        Quaternion endQ   = Quaternion.LookRotation(dir.normalized, Vector3.up);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            rig.rotation   = Quaternion.Slerp(startQ, endQ, elapsed / duration);
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

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>Call after any progress change to refresh all orb visuals and connectors.</summary>
    public void RefreshAllOrbs()
    {
        foreach (var orb in _allOrbs)
            orb.RefreshState();
        UpdateCrossoverConnectors();
    }

    /// <summary>
    /// MVP stub — called by AnteChamberController when the user chooses to complete
    /// the Vestibular programme before a session. The bookmark is stored for future
    /// Baku copy ("You were heading to X — build your tolerance here first").
    /// Full navigation behaviour is post-MVP scope.
    /// </summary>
    public void NavigateToVestibularWithBookmark(SessionData bookmark)
    {
        Debug.Log($"[ConstellationManager] NavigateToVestibularWithBookmark: bookmark={bookmark?.sessionID ?? "null"} (post-MVP navigation — stub)");
    }

    /// <summary>
    /// MVP stub — called by AnteChamberController when the user chooses a different
    /// session. Full zone-expand-on-return behaviour is post-MVP scope.
    /// </summary>
    public void ReturnToConstellationWithZoneExpanded(PhobiaZone zone)
    {
        Debug.Log($"[ConstellationManager] ReturnToConstellationWithZoneExpanded: zone={zone} (post-MVP navigation — stub)");
    }

    /// <summary>
    /// MVP stub — called by ZonePlanet when the user dwells to expand a zone cluster.
    /// Full show/hide of session orbs for that zone is post-MVP scope.
    /// </summary>
    public void ExpandZone(PhobiaZone zone)
    {
        Debug.Log($"[ConstellationManager] ExpandZone: zone={zone} (post-MVP — stub)");
    }

    /// <summary>
    /// MVP stub — called by ZonePlanet when it collapses its zone cluster.
    /// Full hide of session orbs for that zone is post-MVP scope.
    /// </summary>
    public void CollapseZone(PhobiaZone zone)
    {
        Debug.Log($"[ConstellationManager] CollapseZone: zone={zone} (post-MVP — stub)");
    }
}
