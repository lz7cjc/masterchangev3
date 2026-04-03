// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:   3.12
// DATE:      2026-04-02
// TIMESTAMP: 2026-04-02T16:00:00Z
//
// CHANGE LOG:
//   v3.13 2026-04-02  REMOVE zonePlanetPrefab + FIX LABEL COROUTINE
//     - Each zone planet is a distinct FBX placed manually in the scene.
//       zonePlanetPrefab field removed. SpawnZone() now finds the placed
//       ZonePlanet via GetComponentInChildren<ZonePlanet>() on clusterRoot.
//     - SpawnLabel() parentIsActive parameter removed. Now always calls
//       SetVisibleImmediate(true) — coroutine-based Show() was failing
//       because label GameObjects inherit inactive state from their parent
//       at the point of instantiation.
//
//   v3.12 2026-04-02  REMOVE SessionLauncher DEPENDENCY
//     - SessionLauncher was on GameManager (DontDestroyOnLoad), causing its
//       singleton Instance to win over the Video scene's SessionLauncher on
//       VideoPlayerObject — leaving _videoPlayer unassigned and video black.
//     - OnSessionSelected() now calls SessionHandoff.Set() and
//       SceneManager.LoadScene() directly, which is all LaunchSession() did.
//     - SessionLauncher is removed from GameManager entirely.
//     - videoSceneName Inspector field added (default "Video") — matches
//       the field that was previously on SessionLauncher in the Constellation
//       scene role.
//     - SessionLauncher.cs dependency removed from header.
//
//   v3.11 2026-04-02  FIX ORB ORBIT PLANE — COLLIDER CENTRE OFFSET
//   v3.10 2026-04-02  FIX ORB LOCAL POSITION SCALE COMPENSATION
//   v3.9  2026-04-02  ONE ORB PER LEVEL
//   v3.8  2026-04-02  FIX ORB LOCAL POSITION
//   v3.7  2026-04-02  ORBITAL ORB PLACEMENT
//   v3.6  2026-03-22  SESSION ORB LABEL COROUTINE FIX
//   v3.5  2026-03-22  Orb sizing as % of planet + session orb labels.
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
//   ConstellationManager.cs v3.11 (2026-04-02)
//   ConstellationManager.cs v3.10 (2026-04-02)
//   ConstellationManager.cs v3.9  (2026-04-02)
//   ConstellationManager.cs v3.7  (2026-04-02)
//   ConstellationManager.cs v3.6  (2026-03-22)
//   ConstellationManager.cs v3.5  (2026-03-22)
//   ConstellationManager.cs v3.4  (2026-03-19)
//   ConstellationManager.cs v3.3  (2026-03-18)
//   ConstellationManager.cs v3.2  (2026-03-15)
//   ConstellationManager.cs v3.1  (2026-03-15)
//   ConstellationManager.cs v3.0  (2026-03-15)
//   ConstellationManager.cs v2.0  (2026-03-09)
//   ConstellationManager.cs v1    (unversioned)
//
// DEPENDENCIES:
//   SessionRegistry.cs            — GetByPhobiaZone(), GetCrossovers()
//   UserProgressService.cs v2.2.0 — IsCompleted()
//   ZonePlanet.cs v1.0            — zone planet behaviour (gaze → expand/collapse)
//   ConstellationOrb.cs v4.0+     — session orb behaviour (gaze → dwell → launch)
//   ZoneLabelController.cs        — SetLabel(), Show() — used for both zone and orb labels
//   OrbVisuals.cs v1.0            — shared emission helpers
//   SessionHandoff.cs             — Set() — static carrier, no instance needed
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
//   Scene Loading:
//     Video Scene Name   — Must match the scene name in Build Settings (default "Video").
//   Zone Labels:
//     Zone Config        — ZoneConfig asset for zone display names (optional — enum fallback).
//     Label Offset       — Vertical offset above centre for label placement (default 0.3).
//   Orb Sizing:
//     Orb Size As Percent Of Planet — session orb scale as % of planet world scale (default 25).
//   Orb Orbit:
//     Orb Orbit Padding  — extra gap (world units) between planet surface and orb edge (default 0.1).
//   Zone Cluster Entries — one entry per active arc zone. No code change when adding a zone.
//   Crossover Connectors — LineRenderer child objects for Flying↔Heights and Water↔Sharks.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ZoneClusterEntry
{
    [Tooltip("The PhobiaZone enum value this cluster root belongs to")]
    public PhobiaZone zone;

    [Tooltip("Empty GameObject that defines the centre of this zone's cluster")]
    public Transform clusterRoot;
}

/// <summary>
/// ConstellationManager v3.12
///
/// Manages the full two-tier constellation:
///   Tier 1 — Zone planets (ZonePlanet.cs): one per zone, always visible.
///             Gaze → dwell → expands session orbs for that zone.
///   Tier 2 — Session orbs (ConstellationOrb.cs): one per SessionData per zone.
///             Spawn hidden. Visible only when parent zone planet is expanded.
///             Distributed evenly around the planet circumference (orbital plane).
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
    [Tooltip("Session orb prefab — must have ConstellationOrb.cs + GazeHoverTrigger attached. " +
             "Spawned hidden as children of the zone planet. Shown on zone expand.")]
    public GameObject orbPrefab;

    [Tooltip("Zone label prefab — must have ZoneLabelController.cs, CanvasGroup, and TextMeshPro child. " +
             "Used for zone planet labels AND session orb labels. OPTIONAL — leave empty for no labels.")]
    public GameObject labelPrefab;

    // ── Scene loading ─────────────────────────────────────────────────────────
    [Header("Scene Loading")]
    [Tooltip("Exact name of the Video scene as it appears in Build Settings.")]
    public string videoSceneName = "Video";

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

    // ── Orb orbit ─────────────────────────────────────────────────────────────
    [Header("Orb Orbit")]
    [Tooltip("Extra gap (world units) between the planet surface and the nearest edge of each orb. " +
             "Orbit radius = (planetRadius) + (orbRadius) + orbOrbitPadding. " +
             "Collider separation is guaranteed at any value ≥ 0. " +
             "Increase to push orbs further from the planet surface.")]
    [Range(0f, 1f)]
    public float orbOrbitPadding = 0.1f;

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
        if (Instance != null && Instance != this) { Destroy(this); return; }
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

        if (orbPrefab == null)
            Debug.LogError("[ConstellationManager] Orb Prefab is not assigned. " +
                           "Assign the session orb prefab with ConstellationOrb.cs in the Inspector.");

        if (labelPrefab == null)
            Debug.LogWarning("[ConstellationManager] Label Prefab is not assigned — " +
                             "zone planet labels and session orb labels will not appear.");

        Debug.Log($"[ConstellationManager] Building constellation. " +
                  $"Zones: {zoneClusterEntries.Count}. " +
                  $"orbSizeAsPercentOfPlanet: {orbSizeAsPercentOfPlanet}%. " +
                  $"orbOrbitPadding: {orbOrbitPadding}. " +
                  $"labelPrefab: {(labelPrefab != null ? labelPrefab.name : "none")}.");

        foreach (var entry in zoneClusterEntries)
            SpawnZone(entry.zone, entry.clusterRoot);

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built: {_allZonePlanets.Count} zone planets, " +
                  $"{_allOrbs.Count} session orbs across {ZoneClusterCount} zones.");

        FaceStartZone();
    }

    /// <summary>
    /// Finds the manually-placed ZonePlanet under clusterRoot, registers it,
    /// then spawns all session orbs for that zone as hidden children of the planet.
    /// Session orbs are distributed evenly around the planet circumference on a flat
    /// orbital plane. Orbit radius guarantees no collider overlap with the planet.
    /// </summary>
    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No cluster root for {zone} — skipping.");
            return;
        }

        // ── Tier 1: Find the manually-placed ZonePlanet under clusterRoot ────
        ZonePlanet spawnedZonePlanet = clusterRoot.GetComponentInChildren<ZonePlanet>();
        float planetWorldScale = 1f;

        if (spawnedZonePlanet != null)
        {
            spawnedZonePlanet.zone    = zone;
            _allZonePlanets[zone]     = spawnedZonePlanet;
            planetWorldScale          = spawnedZonePlanet.transform.lossyScale.x;

            SpawnLabel(spawnedZonePlanet.transform,
                       zoneConfig != null ? zoneConfig.GetDisplayName(zone) : zone.ToString(),
                       $"Label_{zone}");

            Debug.Log($"[ConstellationManager] Zone planet found for {zone}. " +
                      $"World scale: {spawnedZonePlanet.transform.lossyScale}.");
        }
        else
        {
            Debug.LogError($"[ConstellationManager] No ZonePlanet component found under " +
                           $"cluster root '{clusterRoot.name}' for zone {zone}. " +
                           $"Ensure the placed FBX has ZonePlanet.cs attached.");
        }

        // ── Tier 2: Level orbs (one per level, hidden children of the zone planet) ──
        if (orbPrefab == null)
        {
            _sessionOrbsByZone[zone] = new List<GameObject>();
            return;
        }

        // Derive orb world scale from planet world scale and Inspector percentage
        float orbScale = planetWorldScale * (orbSizeAsPercentOfPlanet / 100f);

        if (spawnedZonePlanet == null)
        {
            orbScale = orbSizeAsPercentOfPlanet / 100f;
            Debug.LogWarning($"[ConstellationManager] {zone} — no zone planet to derive scale from. " +
                             $"Using fallback orb scale: {orbScale:F4}.");
        }
        else
        {
            Debug.Log($"[ConstellationManager] {zone} — planet world scale: {planetWorldScale:F4}, " +
                      $"orb scale: {orbScale:F4} ({orbSizeAsPercentOfPlanet}%).");
        }

        // Read the planet collider's local centre — the mesh pivot is offset from
        // the visual centre (collider centre Y = -9.574 in current prefabs).
        // Orbs must orbit the visual centre, not the transform origin.
        Vector3 colliderCentreLocal = Vector3.zero;
        if (spawnedZonePlanet != null)
        {
            SphereCollider sc = spawnedZonePlanet.GetComponentInChildren<SphereCollider>();
            if (sc != null)
            {
                colliderCentreLocal = sc.center;
                Debug.Log($"[ConstellationManager] {zone} — collider centre local: {colliderCentreLocal}. " +
                          $"Orbs will orbit this point.");
            }
            else
            {
                Debug.LogWarning($"[ConstellationManager] {zone} — no SphereCollider found on zone planet. " +
                                 $"Orbs will orbit transform origin.");
            }
        }

        // Orbit radius: planet collider radius + orb radius + padding
        // Use collider radius if available so radius matches the visible sphere.
        float orbitRadius;
        {
            SphereCollider sc = spawnedZonePlanet != null
                ? spawnedZonePlanet.GetComponentInChildren<SphereCollider>()
                : null;
            float planetRadius = sc != null ? sc.radius * planetWorldScale : planetWorldScale * 0.5f;
            orbitRadius = planetRadius + (orbScale * 0.5f) + orbOrbitPadding;
        }

        // Group all sessions for this zone by level — one orb per level
        List<SessionData> allSessions = SessionRegistry.Instance.GetByPhobiaZone(zone);

        // Build sorted level → pool dictionary
        var levelPools = new SortedDictionary<int, List<SessionData>>();
        foreach (var s in allSessions)
        {
            if (!levelPools.ContainsKey(s.level))
                levelPools[s.level] = new List<SessionData>();
            levelPools[s.level].Add(s);
        }

        Debug.Log($"[ConstellationManager] {zone} — {allSessions.Count} sessions across " +
                  $"{levelPools.Count} levels. orbitRadius: {orbitRadius:F4}.");

        List<GameObject> orbGOs = new List<GameObject>();

        Transform orbParent = spawnedZonePlanet != null
            ? spawnedZonePlanet.transform
            : clusterRoot;

        int total    = levelPools.Count;
        int orbIndex = 0;

        foreach (var kvp in levelPools)
        {
            int                level = kvp.Key;
            List<SessionData>  pool  = kvp.Value;

            // Pick a representative session for state display (first in level order)
            SessionData representative = pool[0];

            Vector3 localPos = OrbitalPosition(orbIndex, total, orbitRadius);

            // Divide world-space orbit vector by parent lossy scale so Unity's
            // local→world multiply restores the correct world-space separation.
            // Add collider centre offset so orbs orbit the visual mesh centre,
            // not the transform pivot (which is offset in AI-generated FBX assets).
            Vector3 parentScale = orbParent.lossyScale;
            Vector3 scaledPos   = new Vector3(
                localPos.x / (parentScale.x != 0f ? parentScale.x : 1f),
                (localPos.y + colliderCentreLocal.y) / (parentScale.y != 0f ? parentScale.y : 1f),
                localPos.z / (parentScale.z != 0f ? parentScale.z : 1f));

            GameObject orbGO = Instantiate(orbPrefab,
                                           orbParent.position,
                                           Quaternion.identity,
                                           orbParent);
            orbGO.transform.localPosition = scaledPos;
            orbGO.name  = $"{zone}_L{level}";
            orbGO.transform.localScale = Vector3.one * orbScale;

            // Start hidden — shown only when zone planet is expanded
            orbGO.SetActive(false);

            ConstellationOrb orb = orbGO.GetComponent<ConstellationOrb>();
            if (orb != null)
            {
                // Assign representative session for state/material — actual session
                // chosen at random from the pool when the orb is selected.
                orb.session = representative;
                orb.sessionPool = pool;
                orb.onSessionSelected.AddListener(OnSessionSelected);
                orb.RefreshState();
                _allOrbs.Add(orb);
            }

            string labelText = $"Level {level}";
            SpawnLabel(orbGO.transform, labelText, $"Label_{zone}_L{level}");

            Debug.Log($"[ConstellationManager] Level orb spawned: {zone} L{level} " +
                      $"index={orbIndex}/{total} pool={pool.Count} sessions " +
                      $"localPos={localPos} scale={orbScale:F4}.");

            orbGOs.Add(orbGO);
            orbIndex++;
        }

        _sessionOrbsByZone[zone] = orbGOs;

        Debug.Log($"[ConstellationManager] SpawnZone complete: {zone} — " +
                  $"{orbGOs.Count} level orbs on orbit radius {orbitRadius:F4}.");
    }

    /// <summary>
    /// Positions session orbs evenly around a full 360° circle at orbitRadius
    /// on the horizontal plane of the planet centre (Y = 0 local space).
    /// All orbs clear the planet collider — orbit radius accounts for both radii
    /// plus the Inspector padding value.
    /// </summary>
    private Vector3 OrbitalPosition(int index, int total, float orbitRadius)
    {
        // Distribute evenly; single orb sits directly in front (angle 0 = +Z)
        float angle = (360f / Mathf.Max(total, 1)) * index;
        float rad   = angle * Mathf.Deg2Rad;
        float x     = Mathf.Sin(rad) * orbitRadius;
        float z     = Mathf.Cos(rad) * orbitRadius;
        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// Instantiates the label prefab as a child of parent, sets its text, and makes it visible.
    /// Uses SetVisibleImmediate() in all cases — coroutine-based Show() is unsafe when the
    /// label GameObject may be inactive at the point of instantiation.
    /// No-op if labelPrefab is null.
    /// </summary>
    private void SpawnLabel(Transform parent, string text, string goName)
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
            label.SetVisibleImmediate(true);

            Debug.Log($"[ConstellationManager] Label spawned: '{goName}' text='{text}'.");
        }
        else
        {
            Debug.LogWarning($"[ConstellationManager] labelPrefab is missing ZoneLabelController " +
                             $"on '{labelGO.name}'.");
        }
    }

    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] Session selected: {session.sessionID}");

        // Route via AntechamberController if present in this scene, otherwise
        // load the Video scene directly (AntechamberController lives in Video scene).
        if (AntechamberController.Instance != null)
        {
            AntechamberController.Instance.ShowForSession(session);
        }
        else
        {
            if (string.IsNullOrEmpty(session.videoURL))
            {
                Debug.LogError($"[ConstellationManager] session.videoURL is empty for " +
                               $"'{session.sessionID}'. Re-import sessions from CSV to populate videoURL.");
                return;
            }

            Debug.Log($"[ConstellationManager] Loading Video scene: '{videoSceneName}' " +
                      $"for session: {session.sessionID}");
            SessionHandoff.Set(session);
            SceneManager.LoadScene(videoSceneName);
        }
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
