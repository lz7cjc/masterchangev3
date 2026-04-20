// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:   3.48
// DATE:      2026-04-19
// TIMESTAMP: 2026-04-19T17:00:00Z
//
// CHANGE LOG:
//   v3.48 2026-04-19  FIX — OnSessionSelected BODY NEVER UPDATED IN v3.47
//     - v3.47 changelog claimed OnSessionSelected() was fixed to call
//       SessionHandoff.Set(session) then SceneManager.LoadScene("Video"),
//       and that using UnityEngine.SceneManagement was added. Neither change
//       was present in the actual file body. Both are applied here.
//     - OnSessionSelected() now: logs session ID, calls SessionHandoff.Set(),
//       calls SceneManager.LoadScene("Video"). AntechamberController/
//       SessionLauncher null-conditional calls removed — both are null in
//       the Constellation scene (they live in the Video scene).
//     - Added using UnityEngine.SceneManagement.
//
// OBSOLETE FILES — DELETE THESE:
//   ConstellationManager.cs v3.47 (2026-04-19)
//   ConstellationManager.cs v3.46 (2026-04-18)
//
//   v3.46 2026-04-18  REAL SESSION ORBS — REPLACE DUMMY SPHERES
//     - SpawnSlotBand now accepts a List<SessionData> parameter.
//     - When sessions are provided and orbPrefab is assigned: instantiates
//       orbPrefab at each slot position (wrapping sessions if count > slots),
//       assigns SessionData to ConstellationOrb.session, calls RefreshState(),
//       wires OnSessionSelected callback.
//     - When sessions is null/empty or orbPrefab is null: falls back to
//       CreatePrimitive (existing Phase 1 dummy behaviour unchanged).
//     - SpawnZone fetches sessions via SessionRegistry.GetByPhobiaZone(),
//       sorts by level, splits evenly across Equator/Upper/Lower bands,
//       passes each band's session slice to SpawnSlotBand.
//     - _sessionOrbsByZone[zone] populated with all real ConstellationOrb GOs.
//     - SwitchLevel calls InitialiseRing (real orbs) instead of
//       InitialiseDummyRing when _sessionOrbsByZone has entries.
//     - All other behaviour unchanged.
//     - SpawnZone: pivot rotation changed from world space (transform.rotation)
//       to local space (transform.localRotation). Previously Quaternion.Euler(
//       pivotEuler) was applied in world space, but WriteRingSegments applies
//       gyroRot in orbParent local space. When orbParent (the planet) has a
//       non-zero world rotation, the two spaces diverge — orbs spawn rotated
//       in world space while ring segments rotate in local space, causing the
//       visible axis mismatch on first expand. Using localRotation keeps both
//       in the same coordinate space. Level switch appeared to fix it because
//       SwitchLevel already used localRotation correctly.
//
//   v3.44 2026-04-18  FIX CS1061 — CONST ORB COUNTS (REAPPLIED)
//     - equatorOrbCount and sideOrbCount are const int 5 and 3 in SpawnZone.
//       This fix was previously delivered in v3.40/v3.42 but the project file
//       was a merge of v3.43 slot fix and the original v3.39 body, so the
//       cfg.equatorOrbCount / cfg.sideOrbCount references remained on lines
//       463-464 causing CS1061. Now definitively fixed in a single clean file.
//
//   v3.43 2026-04-18  FIX — READ SLOTS FROM CONFIG ON SPAWN, NO REBUILD REQUIRED
//     - SpawnZone: eqSlots/upSlots/loSlots now prefer cfg.equatorSlots/upperSlots/
//       lowerSlots when those lists are non-empty. Falls back to mgr.equatorSlots
//       etc. only when the config lists are empty (first-time auto-fill path).
//     - Previously all three slot lists always read from the manager's global
//       fields regardless of what was saved in the config asset. This meant
//       fine-tuned orb positions were ignored on Play Mode entry and only applied
//       after a manual Rebuild (which copied cfg slots into mgr slots first).
//     - Config slot lists are the single source of truth. Manager globals are now
//       only an auto-fill fallback. No Rebuild needed.
//
//   v3.42 2026-04-18  FIX CS1061 — HARDCODE ORB COUNTS (APPLIED)
//     - SwitchLevel: after resetting pivot.localRotation to savedRot, now also
//       calls RotateRingSegmentsForZone(zone, savedRot). Rings are orbParent
//       children — their segment positions are not affected by pivot rotation.
//       When the carousel had been used before SwitchLevel, the pivot was no
//       longer at savedRot. Resetting the pivot snapped orbs back to spawn
//       positions but rings remained at their current (post-carousel) positions,
//       causing visible misalignment. The extra call recomputes ring segments
//       to match the reset pivot so orbs and rings stay aligned on every switch.
//
//   v3.40 2026-04-18  HARDCODE ORB COUNTS — FIX CS1061 COMPILE ERRORS
//     - UpdateRingAlphasForZone now also sets ring line width in addition to
//       alpha. Active ring uses ringLineWidth × activeRingLineWidthMultiplier.
//       Faded rings use ringLineWidth (or per-band override if > 0.001).
//       Previously only alpha changed on level switch — active ring was
//       brighter but not thicker. Width is now updated alongside alpha so
//       the active ring is visually distinct at all times.
//
//   v3.38 2026-04-18  SEPARATE ORB COUNTS + SWITCHLEVEL PIVOT FIX
//     - SpawnZone: reads equatorOrbCount (default 5) and sideOrbCount (default 3)
//       from config separately. Passes equatorOrbCount for equator slots and
//       sideOrbCount for upper/lower slots in EnsureSlotDefaults.
//     - EnsureSlotDefaults: signature updated to accept eqCount and sideCount
//       separately so equator auto-fills with 5 slots and upper/lower with 3.
//     - SwitchLevel: pivot reset changed from Quaternion.identity to
//       Quaternion.Euler(savedPivotEuler). Resetting to identity moved orbs
//       away from the rings when a non-zero gyro euler was saved — orbs spawn
//       rotated by pivotEuler so the pivot must return to that same rotation
//       after a level switch, not to zero.
//
//   v3.37 2026-04-18  FIX RING LONGITUDE OFFSET LOST ON GYRO RECOMPUTE
//     - _liveRings tuple extended with longitudeOffsetDeg (float) — the baked
//       per-band longitude offset stored at spawn time.
//     - RotateRingSegmentsForZone now passes the stored longitudeOffsetDeg to
//       WriteRingSegments instead of 0f. Previously the baked longitude was
//       discarded on every gyro recompute, so rings spawned at the correct
//       longitude but moved to lon=0 after gyro was applied, causing orbs and
//       rings to visually diverge.
//     - UpdateRingAlphasForZone updated to preserve longitudeOffsetDeg when
//       writing back the isActive flag.
//
//   v3.36 2026-04-18  RESTORE RING POSITIONS FROM SAVED GYRO EULER ON SPAWN
//     - SpawnZone: after rings are spawned, if pivotEuler is non-zero, calls
//       RotateRingSegmentsForZone so rings immediately reflect the saved gyro
//       state without requiring a Rebuild. Previously pivot was rotated but
//       ring segments were not recomputed, leaving rings at default positions
//       until a manual Rebuild.
//     - Removed stale warning about non-zero pivotEuler — this is now the
//       correct expected state when a gyro value has been saved.
//
//   v3.35 2026-04-18  PER-BAND RING ROTATION + GYRO MOVES RINGS
//     - SpawnZone: reads equatorRingRotation, upperRingRotation, lowerRingRotation
//       from config (fallback 0). Each is added to longitudeOffsetDeg for that
//       band's SpawnSlotBand and SpawnOrbitRing calls so orbs and ring rotate
//       together. globalRingRotation is also read and added to all three bands.
//     - SpawnOrbitRing: now accepts a longitudeOffsetDeg parameter so the ring
//       circle can be rotated to match its band's orb offset.
//     - _liveRings value tuple extended: now stores (LineRenderer lr, bool isActive,
//       PhobiaZone zone, Transform ringTransform, float orbitRadius, float latitudeDeg,
//       Vector3 pivotLocalPos) so SetOrbPivotRotation can recompute ring segment
//       positions when the gyro rotates, keeping rings in sync with orbs.
//     - SetOrbPivotRotation: after rotating the pivot, iterates _liveRings for
//       the zone and recomputes each ring's segment positions rotated by the
//       same quaternion, so gyro moves orbs and rings together without rebuild.
//
//   v3.34 2026-04-18  FIX RebuildDummyOrbs BAND VISIBILITY + STALE COMMENT
//     - FIX: RebuildDummyOrbs was setting all dummies active when the zone
//       was expanded after a rebuild, showing all three bands simultaneously
//       (same bug as the original ExpandZone). Now mirrors ExpandZone: shows
//       pivot+rings only, then active band orbs only.
//     - FIX: Stale comment in SpawnZone incorrectly stated rings were parented
//       to pivot. Updated to reflect rings are parented to orbParent.
//
//   v3.33 2026-04-18  THREE BUG FIXES
//     - FIX 1: ExpandZone now shows only Equator band orbs on expand (not all
//       three bands). Previously allDummy contained all three bands' orbs so
//       go.SetActive(true) made all bands visible simultaneously, making
//       SwitchLevel appear to do nothing. Fix: ExpandZone iterates
//       _bandOrbsByZone to show only the active band (always Equator on
//       expand since _activeBandByZone resets to 0 on collapse). The pivot
//       and rings (also in allDummy) are shown via the existing allDummy loop
//       filtered to exclude band orbs.
//     - FIX 2: SpawnOrbitRing now parents rings to orbParent (not pivot).
//       Pivot rotation for carousel was spinning all three rings together with
//       the orbs. Rings must be static — only orbs should move. Segment
//       positions are offset by pivot's localPosition within orbParent so
//       rings remain correctly centred on the planet. useWorldSpace remains
//       false (local space of orbParent, not pivot).
//     - FIX 3: SetArrowsForOrbsVisible now applies the ArrowsForOrbs root
//       transform (arrowsRootPosition/Rotation/Scale) from config to the root
//       GameObject before activating it. Previously the root landed at its
//       scene-default position and child arrow local positions were then
//       applied relative to that wrong root position.
//
//   v3.32 2026-04-17  RING REVERT + ARROW INCLUDINACTIVE FIX
//     - REVERT: SpawnOrbitRing restored to original: parented to pivot,
//       localPosition=zero, useWorldSpace=false. The v3.31 approach of
//       parenting rings to orbParent with pivotLocalPos offset was causing
//       displacement because segment positions with useWorldSpace=false are
//       in the ring GO's own local space, and orbParent's scale/rotation
//       affected where they drew. Rings on pivot with localPosition=zero
//       is the original correct approach — a circle rotated around its own
//       Y axis is visually identical. Rings will not visibly spin because
//       the cause of spinning (ZonePlanet gesture code) is already removed.
//     - FIX: SetArrowsForOrbsVisible was calling GetComponentsInChildren
//       with includeInactive:false. Individual arrow GameObjects may be
//       inactive within ArrowsForOrbs. Changed to includeInactive:true so
//       ApplyTransformFromConfig is called on all arrows regardless.
//
//   v3.31 2026-04-16  RINGS DECOUPLED FROM PIVOT (displaced rings — reverted)
//   v3.30 2026-04-16  REVERT RING AND ORB SPAWN TO WORKING STATE
//   v3.27 2026-04-16  ARROWSFORORBS SHOW/HIDE
//   v3.26 2026-04-15  LOCAL ROTATION FOR PIVOT
//   v3.24 2026-04-14  REMOVE CHEVRONS
//   v3.23 2026-04-14  SWITCH LEVEL + DUMMY RING CAROUSEL
//     - FIX: RotateRing / left-right arrow carousel not working.
//       Root cause: _ringState was only populated from _sessionOrbsByZone
//       (real session orbs), which is always empty at P1 dummy-orb stage.
//       Fix: added InitialiseDummyRing(PhobiaZone) that builds OrbRingState
//       directly from the equator dummy GameObjects (no ConstellationOrb
//       component required — uses a lightweight index-only ring).
//       ExpandZone now calls InitialiseDummyRing when _sessionOrbsByZone is
//       empty. TweenRing now rotates the OrbPivot transform (not ZonePlanet)
//       so the orbs physically carousel around the planet while the planet
//       itself stays still.
//     - ADD: SwitchLevel(PhobiaZone, int direction) — up/down arrow handler.
//       direction +1 = Upper band, -1 = Lower band, clamped (no wrap).
//       Tracks active band per zone in _activeBandByZone (0=Equator,+1=Upper,
//       -1=Lower). On switch: hides orbs of the outgoing band, shows orbs of
//       the incoming band, re-initialises the dummy ring for the new band,
//       and updates orbit ring alpha (active/faded). Logs clearly at every step.
//     - ADD: _bandOrbsByZone — Dictionary<PhobiaZone, Dictionary<string,
//       List<GameObject>>> — stores per-band orb lists ("Equator","Upper",
//       "Lower") populated in SpawnZone by capturing all three SpawnSlotBand
//       return values. Upper and Lower band return values were previously
//       discarded.
//     - ADD: _activeBandByZone — Dictionary<PhobiaZone, int> — 0=Equator,
//       +1=Upper, -1=Lower. Initialised to 0 in SpawnZone, reset to 0 in
//       CollapseZoneInternal.
//     - CollapseZoneInternal: resets _activeBandByZone[zone] = 0 on collapse.
//
//   v3.22 2026-04-06  PER-BAND ORBIT RADIUS
//   v3.21 2026-04-06  PER-ZONE CONFIG
//   v3.20 2026-04-06  ORB PIVOT + MULTI-PLANET PREVIEW + PLANET TRANSFORM
//   v3.19 2026-04-06  LIVE LAYOUT EDITOR SUPPORT
//   v3.18 2026-04-05  PHASE 1 VISUAL COMPLETE
//   v3.17–v3.12 see prior headers
//   v3.11–v1    see prior headers
//
// OBSOLETE — DELETE:
//   ConstellationManager.cs v3.45 (2026-04-18)
//   ConstellationManager.cs v3.44 (2026-04-18)
//   ConstellationManager.cs v3.43 (2026-04-18)
//   ConstellationManager.cs v3.42 (2026-04-18)
//   ConstellationManager.cs v3.40 (2026-04-18)
//   ConstellationManager.cs v3.39 (2026-04-18)
//   ConstellationManager.cs v3.37 (2026-04-18)
//   ConstellationManager.cs v3.30 (2026-04-16)
//   ConstellationManager.cs v3.29 (2026-04-16)
//   ConstellationManager.cs v3.28 (2026-04-16)
//   ConstellationManager.cs v3.27 (2026-04-16)
//   ConstellationManager.cs v3.26 (2026-04-15)
//   ConstellationManager.cs v3.24 (2026-04-14)
//   ConstellationManager.cs v3.23 (2026-04-14)
//   ConstellationManager.cs v3.22 (2026-04-06)
//   ConstellationManager.cs v3.21 (2026-04-06)
//   ConstellationManager.cs v3.20 (2026-04-06)
//   ConstellationManager.cs v3.19 (2026-04-06)
//   ConstellationManager.cs v3.18 (2026-04-05)
//   ConstellationManager.cs v3.17 (2026-04-05)
//   ConstellationManager.cs v3.16 (2026-04-05)
//   ConstellationManager.cs v3.15 (2026-04-05)
//   ConstellationManager.cs v3.14 (2026-04-05)
//   ConstellationManager.cs v3.13 (2026-04-04)
//   ConstellationManager.cs v3.12 (2026-04-04)
//   ConstellationManager.cs v3.11 (2026-04-02)
//   ConstellationManager.cs v3.10 (2026-04-02)
//   ConstellationManager.cs v3.9–v1 (see prior headers)
//
// DEPENDENCIES:
//   OrbLayoutConfig.cs v3.1       per-zone config asset
//   SessionRegistry.cs             GetByPhobiaZone(), GetCrossovers()
//   UserProgressService.cs v2.2.0  IsCompleted(), IsLoaded
//   ZonePlanet.cs                  ExpandZone/Collapse callbacks
//   ConstellationOrb.cs v4.4       SetTier() [Phase 2+]
//   ZoneLabelController.cs         SetLabel(), Show(), SetVisibleImmediate()
//   SessionLauncher.cs             LaunchSession()
//   AntechamberController.cs       ShowForSession()
//   PhobiaPriorityManager.cs v1.1  GetStartZone()
//   SessionData.cs                 PhobiaZone enum
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ZoneClusterEntry
{
    [Tooltip("PhobiaZone enum value this cluster belongs to.")]
    public PhobiaZone zone;

    [Tooltip("Empty GameObject that defines the centre of this zone's cluster.")]
    public Transform clusterRoot;

    [Tooltip("Per-zone OrbLayoutConfig asset. All orb geometry for this zone is driven " +
             "by this config. Create one per zone via: Project → right-click → " +
             "Create → MasterChange → Orb Layout Config. " +
             "OR use the 'Create Config' button in the Orb Layout Editor. " +
             "When null, ConstellationManager global defaults are used.")]
    public OrbLayoutConfig layoutConfig;
}

public class ConstellationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConstellationManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { Instance = null; }

    // ── Test / Diagnostics ────────────────────────────────────────────────────
    [Header("Test / Diagnostics")]
    [Tooltip("When set, only this zone spawns. All others skipped. " +
             "NEVER REMOVE — permanent diagnostic tool.")]
    public PhobiaZone testPlanetZone = PhobiaZone.None;

    // ── Prefabs ───────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    public GameObject zonePlanetPrefab;
    public GameObject orbPrefab;
    public GameObject labelPrefab;

    // ── Zone Labels ───────────────────────────────────────────────────────────
    [Header("Zone Labels")]
    public ZoneConfig zoneConfig;

    [Range(0f, 2f)]
    public float labelOffset = 0.3f;

    // ── Global Defaults (used when a zone entry has no layoutConfig assigned) ──
    [Header("Global Defaults — used when zone has no Layout Config assigned")]

    [Header("  Band Slot Positions")]
    public List<Vector2> equatorSlots = new List<Vector2>();
    public List<Vector2> upperSlots   = new List<Vector2>();
    public List<Vector2> lowerSlots   = new List<Vector2>();

    [Range(1, 8)]
    public int bandOrbCount = 3;

    [Header("  Band Layout")]
    public bool moveBandAndOrbsTogether = true;

    [Header("  Band Latitude")]
    [Range(-90f, 90f)] public float equatorLatDeg = 0f;
    [Range(0f,  90f)]  public float upperLatDeg   = 40f;
    [Range(-90f, 0f)]  public float lowerLatDeg   = -40f;

    [Header("  Band Colours")]
    public Color colourCurrent = new Color(0.118f, 0.176f, 0.271f, 1f);
    public Color colourNext    = new Color(0.165f, 0.420f, 0.337f, 1f);
    public Color colourPrev    = new Color(0.227f, 0.675f, 0.741f, 1f);

    [Header("  Orb Sizing")]
    [Range(5f, 100f)] public float orbSizeAsPercentOfPlanet = 25f;
    [Range(0.5f, 3f)] public float orbFrontScale = 1.2f;
    [Range(0.2f, 1.5f)] public float orbSideScale = 0.7f;

    [Header("  Orbit Radius")]
    [Range(1f, 5f)] public float orbitalRadiusMultiplier = 2f;
    [Range(0f, 1f)] public float orbOrbitPadding = 0f;

    [Header("  Orbit Ring Lines")]
    public Color ringColour = Color.white;
    [Range(0.001f, 0.1f)] public float ringLineWidth   = 0.015f;
    [Range(0f, 1f)]       public float ringAlphaActive = 0.9f;
    [Range(0f, 1f)]       public float ringAlphaFaded  = 0.25f;
    [Range(16, 128)]      public int   ringSegments     = 64;

    [Header("  Chevron Affordance")]
    public Color chevronColour = Color.white;
    [Range(0.001f, 0.05f)] public float chevronWidth       = 0.01f;
    [Range(0.1f, 1f)]      public float chevronSizeFraction = 0.5f;

    // ── Ring Navigation (Phase 3+) ────────────────────────────────────────────
    [Header("Ring Navigation — Phase 3+")]
    [Range(0.1f, 1f)] public float ringTweenDuration = 0.3f;

    // ── Zone Clusters ─────────────────────────────────────────────────────────
    [Header("Zone Cluster Entries")]
    [Tooltip("One entry per active zone. Each entry holds its own Layout Config asset.")]
    public List<ZoneClusterEntry> zoneClusterEntries = new List<ZoneClusterEntry>();

    // ── Crossover Connectors ──────────────────────────────────────────────────
    [Header("Crossover Connector Lines")]
    public LineRenderer flyingHeightsConnector;
    public LineRenderer waterSharksConnector;

    // ── Startup Camera ────────────────────────────────────────────────────────
    [Header("Startup Camera")]
    public float cameraFaceDuration = 1.5f;

    // ── Ring state (Phase 3+) ─────────────────────────────────────────────────
    private class OrbRingState
    {
        public List<ConstellationOrb> orderedOrbs;
        public List<GameObject>       dummyOrbs;   // used when orderedOrbs is empty (P1 dummy stage)
        public int frontIndex;
        public OrbRingState(List<ConstellationOrb> orbs, int front)
        { orderedOrbs = orbs; dummyOrbs = null; frontIndex = front; }
        public OrbRingState(List<GameObject> dummies, int front)
        { orderedOrbs = new List<ConstellationOrb>(); dummyOrbs = dummies; frontIndex = front; }
        public int Count => orderedOrbs.Count > 0 ? orderedOrbs.Count
                                                   : (dummyOrbs != null ? dummyOrbs.Count : 0);
    }

    // ── Live-update targets ───────────────────────────────────────────────────
    // Key: "{zone}_{ringName}" e.g. "Flying_Ring_Equator"
    // Tuple stores: LineRenderer, isActive flag, zone, orbitRadius, latitudeDeg,
    // longitudeOffsetDeg (baked at spawn), pivot local position at spawn time —
    // all needed to recompute segments correctly when gyro rotates.
    private Dictionary<string, (LineRenderer lr, bool isActive, PhobiaZone zone,
                                 float orbitRadius, float latitudeDeg,
                                 float longitudeOffsetDeg, Vector3 pivotLocalPos)> _liveRings
        = new Dictionary<string, (LineRenderer, bool, PhobiaZone, float, float, float, Vector3)>();

    private List<(LineRenderer lr, PhobiaZone zone)> _liveChevrons
        = new List<(LineRenderer, PhobiaZone)>();

    // ── Orb Pivot per zone ────────────────────────────────────────────────────
    private Dictionary<PhobiaZone, Transform> _orbPivots
        = new Dictionary<PhobiaZone, Transform>();

    // ── Private state ─────────────────────────────────────────────────────────
    private List<ConstellationOrb>                   _allOrbs             = new List<ConstellationOrb>();
    private Dictionary<PhobiaZone, ZonePlanet>       _allZonePlanets      = new Dictionary<PhobiaZone, ZonePlanet>();
    private Dictionary<PhobiaZone, List<GameObject>> _sessionOrbsByZone   = new Dictionary<PhobiaZone, List<GameObject>>();
    private Dictionary<PhobiaZone, List<GameObject>> _dummyOrbsByZone     = new Dictionary<PhobiaZone, List<GameObject>>();
    private Dictionary<PhobiaZone, OrbRingState>     _ringState           = new Dictionary<PhobiaZone, OrbRingState>();
    private Dictionary<PhobiaZone, Coroutine>        _ringTweenCoroutines = new Dictionary<PhobiaZone, Coroutine>();

    // Per-zone per-band orb lists — "Equator", "Upper", "Lower"
    private Dictionary<PhobiaZone, Dictionary<string, List<GameObject>>> _bandOrbsByZone
        = new Dictionary<PhobiaZone, Dictionary<string, List<GameObject>>>();

    // Active band per zone: 0 = Equator, +1 = Upper, -1 = Lower
    private Dictionary<PhobiaZone, int> _activeBandByZone
        = new Dictionary<PhobiaZone, int>();

    private PhobiaZone _expandedZone = PhobiaZone.None;

    public int ZoneClusterCount
    {
        get { int n = 0; foreach (var e in zoneClusterEntries) if (e.clusterRoot != null) n++; return n; }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (testPlanetZone != PhobiaZone.None)
            Debug.Log($"[ConstellationManager] ⚠ testPlanetZone ACTIVE: {testPlanetZone}.");

        StartCoroutine(BuildConstellation());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private IEnumerator BuildConstellation()
    {
        yield return null;

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found — aborting.");
            yield break;
        }

        float waitTimeout = 10f, waitElapsed = 0f;
        while (UserProgressService.Instance != null &&
               !UserProgressService.Instance.IsLoaded && waitElapsed < waitTimeout)
        { waitElapsed += Time.deltaTime; yield return null; }

        if (waitElapsed >= waitTimeout)
            Debug.LogWarning("[ConstellationManager] Timed out waiting for UserProgressService.");

        Debug.Log($"[ConstellationManager] Building. Zones: {zoneClusterEntries.Count} | " +
                  $"testPlanetZone: {testPlanetZone}.");

        foreach (var entry in zoneClusterEntries)
        {
            Debug.Log($"[ConstellationManager] SpawnZone {entry.zone} — " +
                      $"config: {(entry.layoutConfig != null ? entry.layoutConfig.name : "NULL (using defaults)")}.");
            SpawnZone(entry.zone, entry.clusterRoot, entry.layoutConfig);
        }

        if (testPlanetZone != PhobiaZone.None)
        {
            foreach (var entry in zoneClusterEntries)
            {
                if (entry.clusterRoot == null) continue;
                bool isTest = entry.zone == testPlanetZone;
                entry.clusterRoot.gameObject.SetActive(isTest);
                Debug.Log($"[ConstellationManager] testPlanetZone: {(isTest ? "showing" : "hiding")} {entry.zone}.");
            }
        }

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Build complete. Planets: {_allZonePlanets.Count} | " +
                  $"Pivots: {_orbPivots.Count}.");

        FaceStartZone();
    }

    // ── SpawnZone ─────────────────────────────────────────────────────────────

    private void SpawnZone(PhobiaZone zone, Transform clusterRoot, OrbLayoutConfig cfg)
    {
        if (testPlanetZone != PhobiaZone.None && zone != testPlanetZone)
        {
            Debug.Log($"[ConstellationManager] Skipping {zone} — testPlanetZone={testPlanetZone}.");
            return;
        }

        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No clusterRoot for {zone} — skipping.");
            return;
        }

        // ── Resolve all values from config or fall back to manager defaults ───
        // Slots: prefer saved config slots (fine-tuned positions) over manager globals.
        // Manager globals are only used when config slots are empty — this ensures
        // a Rebuild is never required to apply saved slot positions on Play Mode entry.
        List<Vector2> eqSlots  = (cfg != null && cfg.equatorSlots.Count > 0) ? cfg.equatorSlots : equatorSlots;
        List<Vector2> upSlots  = (cfg != null && cfg.upperSlots.Count   > 0) ? cfg.upperSlots   : upperSlots;
        List<Vector2> loSlots  = (cfg != null && cfg.lowerSlots.Count   > 0) ? cfg.lowerSlots   : lowerSlots;
        int    orbCount        = cfg != null ? cfg.bandOrbCount            : bandOrbCount;
        const int eqOrbCount   = 5;
        const int sideOrbCount = 3;
        bool   moveTogther     = cfg != null ? cfg.moveBandAndOrbsTogether : moveBandAndOrbsTogether;
        float  eqLat           = cfg != null ? cfg.equatorLatDeg           : equatorLatDeg;
        float  upLat           = cfg != null ? cfg.upperLatDeg             : upperLatDeg;
        float  loLat           = cfg != null ? cfg.lowerLatDeg             : lowerLatDeg;
        Color  cCurrent        = cfg != null ? cfg.colourCurrent           : colourCurrent;
        Color  cNext           = cfg != null ? cfg.colourNext              : colourNext;
        Color  cPrev           = cfg != null ? cfg.colourPrev              : colourPrev;
        float  orbSize         = cfg != null ? cfg.orbSizeAsPercentOfPlanet : orbSizeAsPercentOfPlanet;
        float  frontScale      = cfg != null ? cfg.orbFrontScale           : orbFrontScale;
        float  sideScale       = cfg != null ? cfg.orbSideScale            : orbSideScale;
        float  orbitPadding    = cfg != null ? cfg.orbOrbitPadding         : orbOrbitPadding;
        float  eqRadiusMult    = cfg != null ? cfg.equatorRadiusMultiplier : orbitalRadiusMultiplier;
        float  upRadiusMult    = cfg != null ? cfg.upperRadiusMultiplier   : orbitalRadiusMultiplier;
        float  loRadiusMult    = cfg != null ? cfg.lowerRadiusMultiplier   : orbitalRadiusMultiplier;
        Color  rColour         = cfg != null ? cfg.ringColour              : ringColour;
        float  rWidth          = cfg != null ? cfg.ringLineWidth           : ringLineWidth;
        float  rAlphaActive    = cfg != null ? cfg.ringAlphaActive         : ringAlphaActive;
        float  rAlphaFaded     = cfg != null ? cfg.ringAlphaFaded          : ringAlphaFaded;
        int    rSegments       = cfg != null ? cfg.ringSegments            : ringSegments;
        Color  chColour        = cfg != null ? cfg.chevronColour           : chevronColour;
        float  chWidth         = cfg != null ? cfg.chevronWidth            : chevronWidth;
        float  chFraction      = cfg != null ? cfg.chevronSizeFraction     : chevronSizeFraction;
        Vector3 pivotEuler     = cfg != null ? cfg.orbPivotEuler           : Vector3.zero;
        float   globalRot      = cfg != null ? cfg.globalRingRotation      : 0f;
        float   eqRot          = cfg != null ? cfg.equatorRingRotation     : 0f;
        float   upRot          = cfg != null ? cfg.upperRingRotation       : 0f;
        float   loRot          = cfg != null ? cfg.lowerRingRotation       : 0f;

        Debug.Log($"[ConstellationManager] SpawnZone {zone}: " +
                  $"orbSize={orbSize:F1}% eqR={eqRadiusMult:F2} upR={upRadiusMult:F2} loR={loRadiusMult:F2} " +
                  $"eqLat={eqLat:F1}° upLat={upLat:F1}° loLat={loLat:F1}° " +
                  $"config={(cfg != null ? cfg.name : "defaults")}.");

        // ── Zone planet ───────────────────────────────────────────────────────
        ZonePlanet spawnedZonePlanet = null;
        float      planetWorldScale  = 1f;

        ZonePlanet prePlaced = clusterRoot.GetComponentInChildren<ZonePlanet>(includeInactive: true);
        if (prePlaced != null)
        {
            spawnedZonePlanet     = prePlaced;
            prePlaced.zone        = zone;
            _allZonePlanets[zone] = prePlaced;
            planetWorldScale      = prePlaced.transform.lossyScale.x;
            Debug.Log($"[ConstellationManager] Using pre-placed planet for {zone}: " +
                      $"'{prePlaced.gameObject.name}' scale={planetWorldScale:F4}.");
        }
        else if (zonePlanetPrefab != null)
        {
            GameObject planetGO = Instantiate(zonePlanetPrefab, clusterRoot.position,
                                              Quaternion.identity, clusterRoot);
            planetGO.name = $"ZonePlanet_{zone}";
            ZonePlanet zp = planetGO.GetComponent<ZonePlanet>();
            if (zp != null)
            {
                zp.zone               = zone;
                _allZonePlanets[zone] = zp;
                spawnedZonePlanet     = zp;
                planetWorldScale      = planetGO.transform.lossyScale.x;
                Debug.Log($"[ConstellationManager] Planet instantiated: {zone} scale={planetWorldScale:F4}.");
            }
            SpawnLabel(planetGO.transform,
                       zoneConfig != null ? zoneConfig.GetDisplayName(zone) : zone.ToString(),
                       $"Label_{zone}", parentIsActive: true);
        }
        else
        {
            Debug.LogWarning($"[ConstellationManager] No pre-placed planet and no prefab — {zone}.");
        }

        // ── Derive geometry ───────────────────────────────────────────────────
        float colliderRadius = planetWorldScale * 0.5f;
        if (spawnedZonePlanet != null)
        {
            SphereCollider sc = spawnedZonePlanet.GetComponentInChildren<SphereCollider>();
            if (sc != null)
            {
                colliderRadius = sc.radius * spawnedZonePlanet.transform.lossyScale.x;
                Debug.Log($"[ConstellationManager] {zone} colliderRadius={colliderRadius:F4}.");
            }
            else
                Debug.LogWarning($"[ConstellationManager] {zone} — no SphereCollider; using transform origin.");
        }

        float     eqOrbitRadius = (colliderRadius * eqRadiusMult) + orbitPadding;
        float     upOrbitRadius = (colliderRadius * upRadiusMult) + orbitPadding;
        float     loOrbitRadius = (colliderRadius * loRadiusMult) + orbitPadding;
        Transform orbParent     = spawnedZonePlanet != null ? spawnedZonePlanet.transform : clusterRoot;

        // ── Slot defaults ─────────────────────────────────────────────────────
        EnsureSlotDefaults(eqSlots, upSlots, loSlots, eqOrbCount, sideOrbCount, eqLat, upLat, loLat, zone);

        // ── Camera-facing longitude offset ────────────────────────────────────
        // Orbs spawn facing the camera. The offset is computed from camera
        // position at spawn time and added to each slot's longitude. Slot y
        // values do NOT bake this in — they are relative offsets from camera-facing 0°.
        float longitudeOffsetDeg = 0f;
        if (Camera.main != null)
        {
            Vector3 toCam = Camera.main.transform.position - orbParent.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.001f)
            {
                longitudeOffsetDeg = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
                Debug.Log($"[ConstellationManager] {zone}: camera lonOffset={longitudeOffsetDeg:F1}°.");
            }
        }

        // ── Planet visual centre ──────────────────────────────────────────────
        Vector3  planetWorldCentre  = orbParent.position;
        Renderer planetRend         = orbParent.GetComponentInChildren<Renderer>();
        if (planetRend != null) planetWorldCentre = planetRend.bounds.center;

        float planetVisualDiameter = planetRend != null
            ? planetRend.bounds.size.x : colliderRadius * 2f;

        // ── OrbPivot ──────────────────────────────────────────────────────────
        GameObject pivotGO = new GameObject($"OrbPivot_{zone}");
        pivotGO.transform.SetParent(orbParent, worldPositionStays: false);
        pivotGO.transform.position      = planetWorldCentre;
        pivotGO.transform.localRotation = Quaternion.Euler(pivotEuler);
        Transform pivot = pivotGO.transform;
        _orbPivots[zone] = pivot;

        Debug.Log($"[ConstellationManager] OrbPivot_{zone} at {planetWorldCentre:F3} euler={pivotEuler:F1}.");

        // ── Materials ─────────────────────────────────────────────────────────
        Shader   urpUnlit  = Shader.Find("Universal Render Pipeline/Unlit");
        Material matEquator = MakeBandMaterial(urpUnlit, cCurrent, "Equator");
        Material matUpper   = MakeBandMaterial(urpUnlit, cNext,    "Upper");
        Material matLower   = MakeBandMaterial(urpUnlit, cPrev,    "Lower");
        Material matRing    = MakeRingMaterial(urpUnlit, rColour);

        // ── Fetch and distribute sessions across bands ────────────────────────
        // Sessions sorted by level. Split into three groups: lower third → Lower
        // band, middle third → Equator, upper third → Upper. If orbPrefab is null
        // all three calls fall back to dummy primitive behaviour automatically.
        List<SessionData> allSessions = SessionRegistry.Instance != null
            ? SessionRegistry.Instance.GetByPhobiaZone(zone)
                  .OrderBy(s => s.level).ToList()
            : new List<SessionData>();

        List<SessionData> eqSessions, upSessions, loSessions;
        if (allSessions.Count == 0)
        {
            eqSessions = upSessions = loSessions = null;
            Debug.LogWarning($"[ConstellationManager] {zone}: no sessions in registry — using dummy orbs.");
        }
        else
        {
            int total    = allSessions.Count;
            int loEnd    = total / 3;
            int eqEnd    = loEnd + (total - loEnd * 2);   // middle gets any remainder
            loSessions   = allSessions.GetRange(0,     loEnd);
            eqSessions   = allSessions.GetRange(loEnd, eqEnd);
            upSessions   = allSessions.GetRange(loEnd + eqEnd, total - loEnd - eqEnd);
            Debug.Log($"[ConstellationManager] {zone}: {total} sessions → " +
                      $"Lower={loSessions.Count} Equator={eqSessions.Count} Upper={upSessions.Count}.");
        }

        // ── Spawn bands ───────────────────────────────────────────────────────
        List<GameObject> allDummy = new List<GameObject>();
        allDummy.Add(pivotGO);

        List<GameObject> equatorOrbs = SpawnSlotBand(zone, pivot, eqOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            eqLat, eqSlots, longitudeOffsetDeg + globalRot + eqRot, matEquator, "Equator", ref allDummy,
            eqSessions);

        List<GameObject> upperOrbs = SpawnSlotBand(zone, pivot, upOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            upLat, upSlots, longitudeOffsetDeg + globalRot + upRot, matUpper, "Upper", ref allDummy,
            upSessions);

        List<GameObject> lowerOrbs = SpawnSlotBand(zone, pivot, loOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            loLat, loSlots, longitudeOffsetDeg + globalRot + loRot, matLower, "Lower", ref allDummy,
            loSessions);

        // ── Store per-band orb lists ──────────────────────────────────────────
        _bandOrbsByZone[zone] = new Dictionary<string, List<GameObject>>
        {
            { "Equator", equatorOrbs },
            { "Upper",   upperOrbs  },
            { "Lower",   lowerOrbs  }
        };
        _activeBandByZone[zone] = 0; // always start on Equator

        Debug.Log($"[ConstellationManager] {zone} band orb counts: " +
                  $"Equator={equatorOrbs.Count} Upper={upperOrbs.Count} Lower={lowerOrbs.Count}.");

        // ── Spawn rings ───────────────────────────────────────────────────────
        // Rings parented to orbParent (not pivot) so they do not rotate when the
        // carousel fires. Segment positions are offset by the pivot's localPosition
        // within orbParent so rings are correctly centred on the planet.
        float widthMult     = cfg != null ? cfg.activeRingLineWidthMultiplier : 1f;
        float eqSpawnWidth  = cfg != null && cfg.ringLineWidthEquator > 0.001f
                              ? cfg.ringLineWidthEquator * widthMult : rWidth * widthMult;
        float upSpawnWidth  = cfg != null && cfg.ringLineWidthUpper > 0.001f
                              ? cfg.ringLineWidthUpper : rWidth;
        float loSpawnWidth  = cfg != null && cfg.ringLineWidthLower > 0.001f
                              ? cfg.ringLineWidthLower : rWidth;

        SpawnOrbitRing(zone, pivot, orbParent, eqOrbitRadius, eqLat, longitudeOffsetDeg + globalRot + eqRot,
                       rAlphaActive, matRing, rColour, eqSpawnWidth, rSegments, "Ring_Equator", isActive: true, ref allDummy);
        SpawnOrbitRing(zone, pivot, orbParent, upOrbitRadius, upLat, longitudeOffsetDeg + globalRot + upRot,
                       rAlphaFaded, matRing, rColour, upSpawnWidth, rSegments, "Ring_Upper", isActive: false, ref allDummy);
        SpawnOrbitRing(zone, pivot, orbParent, loOrbitRadius, loLat, longitudeOffsetDeg + globalRot + loRot,
                       rAlphaFaded, matRing, rColour, loSpawnWidth, rSegments, "Ring_Lower", isActive: false, ref allDummy);

        // ── Chevrons superseded by ZoneNavArrow system — not spawned ─────────
        // SpawnChevron() method retained but must not be called.

        // ── Restore gyro rotation to rings ────────────────────────────────────
        // pivotEuler is already applied to the pivot transform above so orbs
        // spawn at the correct rotation. Rings are orbParent children and their
        // segments must be explicitly recomputed to match the saved gyro state.
        if (pivotEuler != Vector3.zero)
        {
            RotateRingSegmentsForZone(zone, Quaternion.Euler(pivotEuler));
            Debug.Log($"[ConstellationManager] SpawnZone {zone}: ring segments restored for saved euler={pivotEuler:F1}.");
        }

        _dummyOrbsByZone[zone] = allDummy;

        // Collect all real ConstellationOrb GOs across all three bands.
        // These are used by InitialiseRing and ExpandZone to drive the carousel.
        var realOrbGOs = new List<GameObject>();
        foreach (var go in equatorOrbs.Concat(upperOrbs).Concat(lowerOrbs))
        {
            if (go != null && go.GetComponent<ConstellationOrb>() != null)
                realOrbGOs.Add(go);
        }
        _sessionOrbsByZone[zone] = realOrbGOs;

        if (realOrbGOs.Count > 0)
            Debug.Log($"[ConstellationManager] {zone}: {realOrbGOs.Count} real session orbs registered.");
        else
            Debug.Log($"[ConstellationManager] {zone}: no real orbs — dummy mode active.");

        // Hide dummies (rings + pivotGO children). Real session orbs are hidden
        // separately by ExpandZone/CollapseZone via _bandOrbsByZone show/hide.
        // Do NOT hide real orb GOs here — they start inactive and are shown by
        // ExpandZone when the zone is first opened.
        var realOrbSet = new HashSet<GameObject>(realOrbGOs);
        foreach (var go in allDummy)
            if (go != null && go != pivotGO && !realOrbSet.Contains(go))
                go.SetActive(false);

        // Real orbs also start hidden — shown only when zone expands.
        foreach (var go in realOrbGOs)
            if (go != null) go.SetActive(false);

        Debug.Log($"[ConstellationManager] SpawnZone complete: {zone} — " +
                  $"{allDummy.Count} objects (pivot + orbs + rings).");
    }

    // ── Slot defaults ─────────────────────────────────────────────────────────

    private void EnsureSlotDefaults(List<Vector2> eqSlots, List<Vector2> upSlots,
                                    List<Vector2> loSlots, int eqCount, int sideCount,
                                    float eqLat, float upLat, float loLat, PhobiaZone zone)
    {
        if (eqSlots.Count == 0)
        {
            for (int i = 0; i < eqCount; i++)
                eqSlots.Add(new Vector2(eqLat, (360f / eqCount) * i));
            Debug.Log($"[ConstellationManager] {zone} equatorSlots auto-filled: {eqCount} slots.");
        }
        if (upSlots.Count == 0)
        {
            for (int i = 0; i < sideCount; i++)
                upSlots.Add(new Vector2(upLat, (360f / sideCount) * i));
            Debug.Log($"[ConstellationManager] {zone} upperSlots auto-filled: {sideCount} slots.");
        }
        if (loSlots.Count == 0)
        {
            for (int i = 0; i < sideCount; i++)
                loSlots.Add(new Vector2(loLat, (360f / sideCount) * i));
            Debug.Log($"[ConstellationManager] {zone} lowerSlots auto-filled: {sideCount} slots.");
        }
    }

    // ── Material factories ────────────────────────────────────────────────────

    private Material MakeBandMaterial(Shader urpUnlit, Color colour, string bandName)
    {
        if (urpUnlit == null)
        { Debug.LogWarning($"[ConstellationManager] URP Unlit not found — {bandName} may appear black."); return null; }
        var mat = new Material(urpUnlit);
        mat.SetColor("_BaseColor", colour);
        mat.color = colour;
        return mat;
    }

    private Material MakeRingMaterial(Shader urpUnlit, Color colour)
    {
        if (urpUnlit == null) return null;
        var mat = new Material(urpUnlit);
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend",   0f);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }

    // ── Slot-based band spawn ─────────────────────────────────────────────────
    // sessions: when non-null and orbPrefab is assigned, real ConstellationOrb
    // prefabs are instantiated and SessionData assigned. When null or orbPrefab
    // is missing, falls back to Phase 1 dummy primitive behaviour.
    // Sessions wrap around slots — if sessions.Count > slots.Count the extra
    // sessions repeat slot positions (carousel overflow, hidden until ring rotates).

    private List<GameObject> SpawnSlotBand(PhobiaZone zone, Transform pivot,
                                           float orbitRadius,
                                           float planetVisualDiameter,
                                           float orbSizePct, float frontSc, float sideSc,
                                           float defaultLatDeg,
                                           List<Vector2> slots, float longitudeOffsetDeg,
                                           Material bandMaterial, string bandName,
                                           ref List<GameObject> collector,
                                           List<SessionData> sessions = null)
    {
        float baseOrbScale = planetVisualDiameter * (orbSizePct / 100f);
        var spawned = new List<GameObject>();

        bool useRealOrbs = sessions != null && sessions.Count > 0 && orbPrefab != null;
        int spawnCount   = useRealOrbs ? sessions.Count : slots.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            // Slot position wraps — sessions beyond slot count reuse slot positions.
            int slotIndex = i % slots.Count;

            float latDeg = defaultLatDeg;
            float lonDeg = slots[slotIndex].y + longitudeOffsetDeg;

            if (bandName != "Equator" && Mathf.Abs(slots[slotIndex].x) > 0.01f)
                latDeg = slots[slotIndex].x;

            Vector3 localOffset = OrbitalPositionOnSphere(orbitRadius, latDeg, lonDeg);
            float   orbScale    = baseOrbScale;

            GameObject go;

            if (useRealOrbs)
            {
                // ── Real session orb ─────────────────────────────────────────
                SessionData sd = sessions[i];
                go = Instantiate(orbPrefab, Vector3.zero, Quaternion.identity, pivot);
                go.name = sd.sessionID;
                go.transform.localPosition = localOffset;
                go.transform.localScale    = Vector3.one * orbScale;

                ConstellationOrb orb = go.GetComponent<ConstellationOrb>();
                if (orb != null)
                {
                    // Init must be called before SetTier (via InitialiseRing/ApplyRingTiers).
                    // It captures the runtime spawn scale so _baseScale is correct.
                    orb.Init(Vector3.one * orbScale);
                    orb.session = sd;
                    orb.onSessionSelected.RemoveAllListeners();
                    orb.onSessionSelected.AddListener((_) => OnSessionSelected(sd));
                    orb.RefreshState();
                    _allOrbs.Add(orb);
                    Debug.Log($"[ConstellationManager] {zone} {bandName}[{i}]: " +
                              $"session={sd.sessionID} lat={latDeg:F1}° lon={lonDeg:F1}°.");
                }
                else
                {
                    Debug.LogWarning($"[ConstellationManager] {zone} {bandName}[{i}]: " +
                                     $"orbPrefab missing ConstellationOrb component — session not wired.");
                }
            }
            else
            {
                // ── Phase 1 dummy fallback ────────────────────────────────────
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"P1_{zone}_{bandName}_{i}";
                go.transform.SetParent(pivot, worldPositionStays: false);
                go.transform.localPosition = localOffset;
                go.transform.localScale    = Vector3.one * orbScale;

                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null && bandMaterial != null) rend.sharedMaterial = bandMaterial;

                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Debug.Log($"[ConstellationManager] {zone} {bandName}[{i}] DUMMY: " +
                          $"lat={latDeg:F1}° lon={lonDeg:F1}° scale={orbScale:F3}.");
            }

            spawned.Add(go);
            collector.Add(go);
        }

        Debug.Log($"[ConstellationManager] {zone} Band {bandName}: {spawned.Count} orbs " +
                  $"({(useRealOrbs ? "real" : "dummy")}) orbSize={orbSizePct:F1}% baseScale={baseOrbScale:F4}.");
        return spawned;
    }

    // ── Orbit ring LineRenderer ───────────────────────────────────────────────

    // Rings are parented to orbParent (not the pivot) so they do not rotate
    // when the carousel fires. Segment positions are offset by the pivot's
    // localPosition within orbParent so rings are correctly centred on the planet.
    // longitudeOffsetDeg rotates the ring to match its band's orb offset.
    // Geometry data (orbitRadius, latitudeDeg, pivotLocalPos) is stored in
    // _liveRings so SetOrbPivotRotation can recompute segments when gyro rotates.
    private void SpawnOrbitRing(PhobiaZone zone, Transform pivot, Transform orbParent,
                                float orbitRadius, float latitudeDeg, float longitudeOffsetDeg,
                                float alpha,
                                Material ringMaterial, Color rColour, float rWidth, int rSegs,
                                string ringName, bool isActive, ref List<GameObject> collector)
    {
        if (ringMaterial == null) return;

        GameObject ringGO = new GameObject(ringName);
        ringGO.transform.SetParent(orbParent, worldPositionStays: false);
        ringGO.transform.localPosition = Vector3.zero;
        ringGO.transform.localRotation = Quaternion.identity;

        LineRenderer lr = ringGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = rSegs;
        lr.startWidth    = rWidth;
        lr.endWidth      = rWidth;
        lr.material      = ringMaterial;

        Color c = new Color(rColour.r, rColour.g, rColour.b, alpha);
        lr.startColor = c;
        lr.endColor   = c;

        Vector3 centre = pivot.localPosition;
        WriteRingSegments(lr, centre, orbitRadius, latitudeDeg, longitudeOffsetDeg, rSegs);

        string key = $"{zone}_{ringName}";
        _liveRings[key] = (lr, isActive, zone, orbitRadius, latitudeDeg, longitudeOffsetDeg, centre);

        collector.Add(ringGO);
        Debug.Log($"[ConstellationManager] {zone} Ring {ringName}: lat={latitudeDeg}° " +
                  $"lonOffset={longitudeOffsetDeg:F1}° alpha={alpha:F2} centre={centre:F3}.");
    }

    /// <summary>
    /// Writes ring segment positions to a LineRenderer.
    /// centre: pivot local position within orbParent (ring is centred here).
    /// gyroRot: optional additional rotation applied to each segment position
    /// around the centre — used by SetOrbPivotRotation to keep rings aligned with orbs.
    /// </summary>
    private void WriteRingSegments(LineRenderer lr, Vector3 centre,
                                   float orbitRadius, float latitudeDeg,
                                   float longitudeOffsetDeg, int rSegs,
                                   Quaternion? gyroRot = null)
    {
        float latRad = latitudeDeg * Mathf.Deg2Rad;
        float y = orbitRadius * Mathf.Sin(latRad);
        float r = orbitRadius * Mathf.Cos(latRad);
        float lonOffsetRad = longitudeOffsetDeg * Mathf.Deg2Rad;

        for (int i = 0; i < rSegs; i++)
        {
            float lonRad = (360f / rSegs) * i * Mathf.Deg2Rad + lonOffsetRad;
            Vector3 seg  = new Vector3(Mathf.Sin(lonRad) * r, y, Mathf.Cos(lonRad) * r);
            if (gyroRot.HasValue)
                seg = gyroRot.Value * seg;
            lr.SetPosition(i, centre + seg);
        }
    }

    // ── Chevron affordance ────────────────────────────────────────────────────

    private void SpawnChevron(PhobiaZone zone, GameObject sideOrb,
                              float direction, Color chColour, float chWidth, float chFraction,
                              string chevronName, ref List<GameObject> collector)
    {
        if (sideOrb == null) return;

        float   orbScale = sideOrb.transform.lossyScale.x;
        float   halfSize = orbScale * chFraction * 0.5f;
        Vector3 localOrb = sideOrb.transform.localPosition;
        Vector3 inward   = -localOrb; inward.y = 0f;
        if (inward.sqrMagnitude < 0.001f) return;
        inward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, inward) * direction;
        Vector3 tip   = localOrb + inward * halfSize;
        Vector3 armA  = localOrb + right  * halfSize;
        Vector3 armB  = localOrb - right  * halfSize;

        GameObject chevGO = new GameObject(chevronName);
        chevGO.transform.SetParent(sideOrb.transform.parent, worldPositionStays: false);
        chevGO.transform.localPosition = Vector3.zero;

        LineRenderer lr = chevGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = false;
        lr.positionCount = 3;
        lr.startWidth    = chWidth;
        lr.endWidth      = chWidth;
        lr.SetPosition(0, armA);
        lr.SetPosition(1, tip);
        lr.SetPosition(2, armB);
        lr.startColor = chColour;
        lr.endColor   = chColour;

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit != null)
        {
            var mat = new Material(urpUnlit);
            mat.SetColor("_BaseColor", chColour);
            lr.material = mat;
        }

        _liveChevrons.Add((lr, zone));
        collector.Add(chevGO);
        Debug.Log($"[ConstellationManager] {zone} Chevron {chevronName} halfSize={halfSize:F3}.");
    }

    // ── Orbital position ──────────────────────────────────────────────────────

    private Vector3 OrbitalPositionOnSphere(float orbitRadius, float latitudeDeg, float longitudeDeg)
    {
        float latRad = latitudeDeg  * Mathf.Deg2Rad;
        float lonRad = longitudeDeg * Mathf.Deg2Rad;
        float y = orbitRadius * Mathf.Sin(latRad);
        float r = orbitRadius * Mathf.Cos(latRad);
        return new Vector3(Mathf.Sin(lonRad) * r, y, Mathf.Cos(lonRad) * r);
    }

    // ── Per-zone config lookup ────────────────────────────────────────────────

    /// <summary>Returns the layoutConfig for a zone, or null if not assigned.</summary>
    public OrbLayoutConfig GetConfigForZone(PhobiaZone zone)
    {
        foreach (var entry in zoneClusterEntries)
            if (entry.zone == zone) return entry.layoutConfig;
        return null;
    }

    /// <summary>Assigns a config asset to a zone entry.</summary>
    public void SetConfigForZone(PhobiaZone zone, OrbLayoutConfig cfg)
    {
        foreach (var entry in zoneClusterEntries)
        {
            if (entry.zone == zone)
            {
                entry.layoutConfig = cfg;
                Debug.Log($"[ConstellationManager] SetConfigForZone: {zone} → {(cfg != null ? cfg.name : "null")}.");
                return;
            }
        }
        Debug.LogWarning($"[ConstellationManager] SetConfigForZone: zone {zone} not found in entries.");
    }

    // ── Gyroscope pivot ───────────────────────────────────────────────────────

    public void SetOrbPivotRotation(PhobiaZone zone, Quaternion rotation)
    {
        if (_orbPivots.TryGetValue(zone, out Transform pivot) && pivot != null)
        {
            pivot.localRotation = rotation;
            Debug.Log($"[ConstellationManager] SetOrbPivotRotation: {zone} localEuler={rotation.eulerAngles:F1}.");
            RotateRingSegmentsForZone(zone, rotation);
        }
        else
            Debug.LogWarning($"[ConstellationManager] SetOrbPivotRotation: no pivot for {zone}.");
    }

    public void SetAllOrbPivotRotation(Quaternion rotation)
    {
        int count = 0;
        foreach (var kvp in _orbPivots)
        {
            if (kvp.Value != null)
            {
                kvp.Value.localRotation = rotation;
                RotateRingSegmentsForZone(kvp.Key, rotation);
                count++;
            }
        }
        Debug.Log($"[ConstellationManager] SetAllOrbPivotRotation: localEuler={rotation.eulerAngles:F1} × {count} pivots.");
    }

    /// <summary>
    /// Recomputes all ring segment positions for a zone using the supplied gyro rotation.
    /// Called by SetOrbPivotRotation so rings stay in sync with orbs when gyro changes.
    /// </summary>
    private void RotateRingSegmentsForZone(PhobiaZone zone, Quaternion gyroRot)
    {
        OrbLayoutConfig cfg = GetConfigForZone(zone);
        int rSegs = cfg != null ? cfg.ringSegments : ringSegments;

        foreach (var kvp in _liveRings)
        {
            if (kvp.Value.zone != zone) continue;
            var (lr, _, _, orbitRadius, latitudeDeg, longitudeOffsetDeg, pivotLocalPos) = kvp.Value;
            if (lr == null) continue;
            WriteRingSegments(lr, pivotLocalPos, orbitRadius, latitudeDeg, longitudeOffsetDeg, rSegs, gyroRot);
            Debug.Log($"[ConstellationManager] RotateRingSegments: {kvp.Key} recomputed lonOffset={longitudeOffsetDeg:F1}° gyro={gyroRot.eulerAngles:F1}.");
        }
    }

    public Vector3 GetOrbPivotEuler(PhobiaZone zone)
    {
        if (_orbPivots.TryGetValue(zone, out Transform pivot) && pivot != null)
            return pivot.localRotation.eulerAngles;
        return Vector3.zero;
    }

    // ── Planet transform ──────────────────────────────────────────────────────

    public void SavePlanetTransformToConfig(PhobiaZone zone, OrbLayoutConfig cfg)
    {
        if (cfg == null) { Debug.LogWarning("[ConstellationManager] SavePlanetTransform: cfg null."); return; }
        if (!_allZonePlanets.TryGetValue(zone, out ZonePlanet zp) || zp == null)
        { Debug.LogWarning($"[ConstellationManager] SavePlanetTransform: no planet for {zone}."); return; }
        cfg.planetPosition = zp.transform.position;
        cfg.planetRotation = zp.transform.eulerAngles;
        cfg.planetScale    = zp.transform.localScale;
        Debug.Log($"[ConstellationManager] SavePlanetTransform: {zone} " +
                  $"pos={cfg.planetPosition:F3} rot={cfg.planetRotation:F1} scale={cfg.planetScale:F3}.");
    }

    public void ApplyPlanetTransformFromConfig(PhobiaZone zone, OrbLayoutConfig cfg)
    {
        if (cfg == null) { Debug.LogWarning("[ConstellationManager] ApplyPlanetTransform: cfg null."); return; }
        if (!_allZonePlanets.TryGetValue(zone, out ZonePlanet zp) || zp == null)
        { Debug.LogWarning($"[ConstellationManager] ApplyPlanetTransform: no planet for {zone}."); return; }
        zp.transform.position    = cfg.planetPosition;
        zp.transform.eulerAngles = cfg.planetRotation;
        zp.transform.localScale  = cfg.planetScale;
        Debug.Log($"[ConstellationManager] ApplyPlanetTransform: {zone} applied.");
    }

    // ── Multi-planet show/hide ────────────────────────────────────────────────

    public void SetZoneVisible(PhobiaZone zone, bool visible, OrbLayoutConfig cfg)
    {
        foreach (var entry in zoneClusterEntries)
        {
            if (entry.zone != zone || entry.clusterRoot == null) continue;
            entry.clusterRoot.gameObject.SetActive(visible);
            Debug.Log($"[ConstellationManager] SetZoneVisible: {zone} → {visible}.");
            break;
        }
        if (cfg != null)
        {
            if (visible && !cfg.visibleZones.Contains(zone)) cfg.visibleZones.Add(zone);
            else if (!visible) cfg.visibleZones.Remove(zone);
        }
    }

    // ── Live property update (zone-scoped) ────────────────────────────────────

    public void ApplyLiveProperties(PhobiaZone zone, OrbLayoutConfig cfg)
    {
        if (cfg == null) { Debug.LogWarning("[ConstellationManager] ApplyLiveProperties: cfg null."); return; }

        int ringsUpdated = 0, chevsUpdated = 0;

        foreach (var kvp in _liveRings)
        {
            if (kvp.Value.zone != zone) continue;
            LineRenderer lr = kvp.Value.lr;
            if (lr == null) continue;
            float alpha = kvp.Value.isActive ? cfg.ringAlphaActive : cfg.ringAlphaFaded;
            Color c = new Color(cfg.ringColour.r, cfg.ringColour.g, cfg.ringColour.b, alpha);
            lr.startColor = c; lr.endColor = c;
            lr.startWidth = cfg.ringLineWidth; lr.endWidth = cfg.ringLineWidth;
            ringsUpdated++;
        }

        foreach (var t in _liveChevrons)
        {
            if (t.zone != zone || t.lr == null) continue;
            t.lr.startColor = cfg.chevronColour; t.lr.endColor = cfg.chevronColour;
            t.lr.startWidth = cfg.chevronWidth;  t.lr.endWidth = cfg.chevronWidth;
            chevsUpdated++;
        }

        Debug.Log($"[ConstellationManager] ApplyLiveProperties: {zone} " +
                  $"rings={ringsUpdated} chevrons={chevsUpdated}.");
    }

    // ── Runtime rebuild ───────────────────────────────────────────────────────

    [ContextMenu("Rebuild Dummy Orbs (Test Planet)")]
    public void RebuildAndResetSlots()
    {
        _liveRings.Clear();
        _liveChevrons.Clear();
        _orbPivots.Clear();

        PhobiaZone zone = testPlanetZone != PhobiaZone.None ? testPlanetZone : _expandedZone;
        OrbLayoutConfig cfg = GetConfigForZone(zone);

        bool moveTogether = cfg != null ? cfg.moveBandAndOrbsTogether : moveBandAndOrbsTogether;
        if (moveTogether)
        {
            equatorSlots.Clear(); upperSlots.Clear(); lowerSlots.Clear();
            Debug.Log("[ConstellationManager] RebuildAndResetSlots: slots cleared.");
        }

        RebuildDummyOrbs(zone);
    }

    public void RebuildDummyOrbs(PhobiaZone zone)
    {
        if (zone == PhobiaZone.None)
        {
            Debug.LogWarning("[ConstellationManager] RebuildDummyOrbs: zone is None — nothing to rebuild.");
            return;
        }

        var keysToRemove = new List<string>();
        foreach (var kvp in _liveRings)
            if (kvp.Value.zone == zone) keysToRemove.Add(kvp.Key);
        foreach (var k in keysToRemove) _liveRings.Remove(k);
        _liveChevrons.RemoveAll(t => t.zone == zone);
        _orbPivots.Remove(zone);
        _bandOrbsByZone.Remove(zone);
        _activeBandByZone.Remove(zone);

        if (_dummyOrbsByZone.TryGetValue(zone, out List<GameObject> existing))
        {
            foreach (var go in existing) if (go != null) DestroyImmediate(go);
            _dummyOrbsByZone.Remove(zone);
        }

        foreach (var entry in zoneClusterEntries)
        {
            if (entry.zone != zone || entry.clusterRoot == null) continue;

            Debug.Log($"[ConstellationManager] RebuildDummyOrbs: {zone} " +
                      $"config={(entry.layoutConfig != null ? entry.layoutConfig.name : "null (defaults)")}.");

            SpawnZone(zone, entry.clusterRoot, entry.layoutConfig);

            if (_expandedZone == zone && _dummyOrbsByZone.TryGetValue(zone, out var newOrbs))
            {
                // Mirror ExpandZone: show pivot+rings only, then active band orbs only.
                var allBandOrbsRebuild = new HashSet<GameObject>();
                if (_bandOrbsByZone.TryGetValue(zone, out var rebuildBands))
                    foreach (var kvp in rebuildBands)
                        foreach (var go in kvp.Value)
                            if (go != null) allBandOrbsRebuild.Add(go);

                foreach (var go in newOrbs)
                    if (go != null && !allBandOrbsRebuild.Contains(go))
                        go.SetActive(true);

                string activeBandRebuild = ActiveBandName(zone);
                if (rebuildBands != null && rebuildBands.TryGetValue(activeBandRebuild, out var activeOrbsRebuild))
                    foreach (var go in activeOrbsRebuild) if (go != null) go.SetActive(true);

                Debug.Log($"[ConstellationManager] RebuildDummyOrbs: {zone} re-shown, active band={activeBandRebuild}.");
            }

            Debug.Log($"[ConstellationManager] RebuildDummyOrbs complete: {zone}. " +
                      $"Pivots={_orbPivots.Count} Rings={_liveRings.Count} Chevrons={_liveChevrons.Count}.");
            return;
        }

        Debug.LogWarning($"[ConstellationManager] RebuildDummyOrbs: zone {zone} not found in entries.");
    }

    // ── Expand / Collapse ─────────────────────────────────────────────────────

    public void ExpandZone(PhobiaZone zone)
    {
        if (_expandedZone != PhobiaZone.None && _expandedZone != zone)
            CollapseZoneInternal(_expandedZone, notifyZonePlanet: true);

        _expandedZone = zone;

        // Show pivot and rings (non-band objects in allDummy) plus only the
        // active band's orbs. All three bands were hidden at spawn — showing
        // all dummies would make all bands visible simultaneously, breaking SwitchLevel.
        if (_dummyOrbsByZone.TryGetValue(zone, out List<GameObject> dummies))
        {
            // Collect all band orb GameObjects so we can exclude them from the bulk show
            var allBandOrbs = new HashSet<GameObject>();
            if (_bandOrbsByZone.TryGetValue(zone, out var bands))
                foreach (var kvp in bands)
                    foreach (var go in kvp.Value)
                        if (go != null) allBandOrbs.Add(go);

            // Show pivot and rings
            foreach (var go in dummies)
                if (go != null && !allBandOrbs.Contains(go))
                    go.SetActive(true);

            // Show only the active band (always Equator on first expand)
            string activeBand = ActiveBandName(zone);
            if (bands != null && bands.TryGetValue(activeBand, out var activeOrbs))
            {
                foreach (var go in activeOrbs) if (go != null) go.SetActive(true);
                Debug.Log($"[ConstellationManager] ExpandZone {zone}: showing {activeOrbs.Count} {activeBand} orbs.");
            }

            Debug.Log($"[ConstellationManager] Expanded {zone}: pivot+rings shown, active band={activeBand}.");
        }

        // Show the ArrowsForOrbs root for this zone.
        SetArrowsForOrbsVisible(zone, true);

        // Initialise ring from real session orbs if available, otherwise from dummy equator orbs.
        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> realOrbs) && realOrbs.Count > 0)
        {
            InitialiseRing(zone);
            Debug.Log($"[ConstellationManager] ExpandZone {zone}: ring initialised from {realOrbs.Count} real orbs.");
        }
        else
        {
            InitialiseDummyRing(zone);
        }

        if (!_dummyOrbsByZone.ContainsKey(zone) && !_sessionOrbsByZone.ContainsKey(zone))
            Debug.LogWarning($"[ConstellationManager] ExpandZone: no orbs for {zone}.");
    }

    public void CollapseZone(PhobiaZone zone) => CollapseZoneInternal(zone, notifyZonePlanet: false);

    private void CollapseZoneInternal(PhobiaZone zone, bool notifyZonePlanet)
    {
        if (_ringTweenCoroutines.TryGetValue(zone, out Coroutine c) && c != null)
        { StopCoroutine(c); _ringTweenCoroutines[zone] = null; }

        if (_dummyOrbsByZone.TryGetValue(zone, out List<GameObject> dummies))
            foreach (var go in dummies) if (go != null) go.SetActive(false);
        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> realOrbs))
            foreach (var go in realOrbs) if (go != null) go.SetActive(false);

        // Hide the ArrowsForOrbs root for this zone.
        SetArrowsForOrbsVisible(zone, false);

        _ringState.Remove(zone);
        _activeBandByZone[zone] = 0; // reset to Equator on collapse

        if (notifyZonePlanet && _allZonePlanets.TryGetValue(zone, out ZonePlanet zp))
            zp.Collapse();

        if (_expandedZone == zone) _expandedZone = PhobiaZone.None;
        Debug.Log($"[ConstellationManager] Collapsed: {zone}.");
    }

    /// <summary>
    /// Finds the ArrowsForOrbs root GameObject under the zone's cluster root
    /// and shows or hides it. When showing, applies saved transforms from the
    /// zone's OrbLayoutConfig to each ZoneNavArrow child so positions are
    /// correct on the first expand without needing a manual Apply press.
    /// </summary>
    private void SetArrowsForOrbsVisible(PhobiaZone zone, bool visible)
    {
        foreach (var entry in zoneClusterEntries)
        {
            if (entry.zone != zone || entry.clusterRoot == null) continue;

            foreach (Transform t in entry.clusterRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t == entry.clusterRoot) continue;
                if (string.Equals(t.name, "ArrowsForOrbs", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (visible)
                    {
                        // Apply root transform from config BEFORE activating so the
                        // root lands at its saved position, not its scene-default position.
                        OrbLayoutConfig cfg = GetConfigForZone(zone);
                        if (cfg != null)
                        {
                            if (cfg.arrowsRootPosition != Vector3.zero)
                                t.localPosition    = cfg.arrowsRootPosition;
                            if (cfg.arrowsRootRotation != Vector3.zero)
                                t.localEulerAngles = cfg.arrowsRootRotation;
                            if (cfg.arrowsRootScale != Vector3.one)
                                t.localScale       = cfg.arrowsRootScale;
                            Debug.Log($"[ConstellationManager] SetArrowsForOrbsVisible: {zone} root transform applied " +
                                      $"pos={cfg.arrowsRootPosition} rot={cfg.arrowsRootRotation} scale={cfg.arrowsRootScale}.");

                            // Apply individual arrow transforms
                            foreach (var arrow in t.GetComponentsInChildren<ZoneNavArrow>(includeInactive: true))
                                arrow.ApplyTransformFromConfig(cfg);
                        }
                        else
                        {
                            Debug.LogWarning($"[ConstellationManager] SetArrowsForOrbsVisible: no config for {zone} — transforms not applied.");
                        }
                    }

                    t.gameObject.SetActive(visible);
                    Debug.Log($"[ConstellationManager] SetArrowsForOrbsVisible: {zone} → {visible}.");
                    return;
                }
            }

            Debug.LogWarning($"[ConstellationManager] SetArrowsForOrbsVisible: 'ArrowsForOrbs' not found under {entry.clusterRoot.name}.");
            return;
        }
    }

    // ── Ring management (Phase 3+) ────────────────────────────────────────────

    private void InitialiseRing(PhobiaZone zone)
    {
        if (!_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbGOs)) return;
        var orbs = new List<ConstellationOrb>();
        foreach (var go in orbGOs) { if (go == null) continue; var o = go.GetComponent<ConstellationOrb>(); if (o != null) orbs.Add(o); }
        if (orbs.Count == 0) { Debug.LogWarning($"[ConstellationManager] InitialiseRing: no ConstellationOrb components for {zone}."); return; }

        int front = 0;
        for (int i = 0; i < orbs.Count; i++)
        {
            string sid = orbs[i].session?.sessionID;
            if (sid != null && !UserProgressService.Instance.IsCompleted(sid)) { front = i; break; }
        }
        _ringState[zone] = new OrbRingState(orbs, front);
        ApplyRingTiers(zone);
        Debug.Log($"[ConstellationManager] InitialiseRing: {zone} front={front}/{orbs.Count}.");
    }

    /// <summary>
    /// Builds a lightweight ring state from the dummy orbs of the currently active band.
    /// Used at P1 stage when real session orbs are not yet loaded. No ConstellationOrb
    /// component required — the tween rotates the OrbPivot physically.
    /// </summary>
    private void InitialiseDummyRing(PhobiaZone zone)
    {
        if (!_bandOrbsByZone.TryGetValue(zone, out var bands))
        {
            Debug.LogWarning($"[ConstellationManager] InitialiseDummyRing: no band data for {zone}.");
            return;
        }

        string activeBand = ActiveBandName(zone);
        if (!bands.TryGetValue(activeBand, out List<GameObject> orbGOs) || orbGOs.Count == 0)
        {
            Debug.LogWarning($"[ConstellationManager] InitialiseDummyRing: no dummy orbs for {zone} band={activeBand}.");
            return;
        }

        _ringState[zone] = new OrbRingState(orbGOs, front: 0);
        Debug.Log($"[ConstellationManager] InitialiseDummyRing: {zone} band={activeBand} orbs={orbGOs.Count}.");
    }

    private void ApplyRingTiers(PhobiaZone zone)
    {
        if (!_ringState.TryGetValue(zone, out OrbRingState ring)) return;

        // Real ConstellationOrb path (Phase 2+)
        if (ring.orderedOrbs.Count > 0)
        {
            int count = ring.orderedOrbs.Count;
            for (int i = 0; i < count; i++)
            {
                var orb = ring.orderedOrbs[i]; if (orb == null) continue;
                int offset = i - ring.frontIndex;
                while (offset >  count / 2) offset -= count;
                while (offset < -count / 2) offset += count;
                int abs = Mathf.Abs(offset);
                orb.SetTier(abs == 0 ? ConstellationOrb.OrbTier.Front   :
                            abs == 1 ? ConstellationOrb.OrbTier.SideNear :
                            abs == 2 ? ConstellationOrb.OrbTier.SideFar  :
                                       ConstellationOrb.OrbTier.Hidden);
            }
        }
        // Dummy orb path (Phase 1) — no tier system, all dummies stay visible; tween handles visual
    }

    public void RotateRing(PhobiaZone zone, int direction)
    {
        if (!_ringState.TryGetValue(zone, out OrbRingState ring))
        {
            Debug.LogWarning($"[ConstellationManager] RotateRing: no ring state for {zone}. " +
                             $"Zone expanded={_expandedZone} — was ExpandZone called?");
            return;
        }

        int count = ring.Count;
        if (count <= 1)
        {
            Debug.Log($"[ConstellationManager] RotateRing: {zone} only {count} orb(s) — nothing to rotate.");
            return;
        }

        if (_ringTweenCoroutines.TryGetValue(zone, out Coroutine ex) && ex != null) StopCoroutine(ex);

        int newFront = (ring.frontIndex + direction + count) % count;
        ring.frontIndex = newFront;

        // Step angle: positive direction = orbs move left (pivot rotates right).
        float stepAngle = 360f / count * -direction;

        _ringTweenCoroutines[zone] = StartCoroutine(TweenRing(zone, stepAngle));
        Debug.Log($"[ConstellationManager] RotateRing: {zone} dir={direction} front→{newFront} " +
                  $"stepAngle={stepAngle:F1}° count={count}.");
    }

    private IEnumerator TweenRing(PhobiaZone zone, float stepAngle)
    {
        // Rotate the OrbPivot in LOCAL space — orbs orbit around the planet.
        // Using world rotation (pivot.rotation) would swing orbs around the
        // world origin when the planet is not at (0,0,0).
        if (!_orbPivots.TryGetValue(zone, out Transform pivot) || pivot == null)
        {
            Debug.LogWarning($"[ConstellationManager] TweenRing: no OrbPivot for {zone}.");
            yield break;
        }

        Quaternion startRot = pivot.localRotation;
        // Rotate around the pivot's local Y axis — keeps the ring horizontal.
        Quaternion stepRot  = Quaternion.AngleAxis(stepAngle, Vector3.up);
        Quaternion endRot   = startRot * stepRot;
        float elapsed = 0f;

        Debug.Log($"[ConstellationManager] TweenRing start: {zone} pivot={pivot.name} " +
                  $"localFrom={startRot.eulerAngles:F1} stepAngle={stepAngle:F1}°.");

        while (elapsed < ringTweenDuration)
        {
            elapsed += Time.deltaTime;
            float tt = Mathf.Clamp01(elapsed / ringTweenDuration);
            // Smoothstep easing
            pivot.localRotation = Quaternion.Slerp(startRot, endRot, tt * tt * (3f - 2f * tt));
            yield return null;
        }

        pivot.localRotation = endRot;
        ApplyRingTiers(zone);
        _ringTweenCoroutines[zone] = null;

        Debug.Log($"[ConstellationManager] TweenRing complete: {zone} localRot={pivot.localRotation.eulerAngles:F1}.");
    }

    // ── Level switching (up/down arrows) ─────────────────────────────────────

    /// <summary>
    /// Switches the active band for the given zone.
    /// direction +1 = move up to Upper band.
    /// direction -1 = move down to Lower band.
    /// Clamped: Upper is the ceiling, Lower is the floor — no wrap.
    /// Called by ZonePlanet (vertical gaze gesture) and ZoneNavArrow (up/down arrows).
    /// </summary>
    public void SwitchLevel(PhobiaZone zone, int direction)
    {
        if (!_bandOrbsByZone.TryGetValue(zone, out var bands))
        {
            Debug.LogWarning($"[ConstellationManager] SwitchLevel: no band data for {zone}.");
            return;
        }

        int current = _activeBandByZone.TryGetValue(zone, out int v) ? v : 0;
        int desired = Mathf.Clamp(current + direction, -1, 1);

        if (desired == current)
        {
            Debug.Log($"[ConstellationManager] SwitchLevel: {zone} already at " +
                      $"{ActiveBandName(zone)} — clamped, no change.");
            return;
        }

        string fromBand = ActiveBandName(zone);
        _activeBandByZone[zone] = desired;
        string toBand = ActiveBandName(zone);

        Debug.Log($"[ConstellationManager] SwitchLevel: {zone} {fromBand} → {toBand} " +
                  $"(dir={direction} current={current} desired={desired}).");

        // ── Hide outgoing band orbs ───────────────────────────────────────────
        if (bands.TryGetValue(fromBand, out var outOrbs))
        {
            foreach (var go in outOrbs) if (go != null) go.SetActive(false);
            Debug.Log($"[ConstellationManager] SwitchLevel: hidden {outOrbs.Count} {fromBand} orbs.");
        }

        // ── Show incoming band orbs ───────────────────────────────────────────
        if (bands.TryGetValue(toBand, out var inOrbs))
        {
            foreach (var go in inOrbs) if (go != null) go.SetActive(true);
            Debug.Log($"[ConstellationManager] SwitchLevel: shown {inOrbs.Count} {toBand} orbs.");
        }
        else
        {
            Debug.LogWarning($"[ConstellationManager] SwitchLevel: no orbs found for band {toBand} on {zone}.");
        }

        // ── Re-initialise ring for new active band ────────────────────────────
        // Reset the pivot rotation to the saved gyro euler so orbs return to
        // their spawn positions relative to the rings. Using Quaternion.identity
        // would move orbs away from the rings when a non-zero gyro is saved.
        if (_orbPivots.TryGetValue(zone, out Transform pivot) && pivot != null)
        {
            OrbLayoutConfig cfg = GetConfigForZone(zone);
            Quaternion savedRot = cfg != null ? Quaternion.Euler(cfg.orbPivotEuler) : Quaternion.identity;
            pivot.localRotation = savedRot;
            RotateRingSegmentsForZone(zone, savedRot);
            Debug.Log($"[ConstellationManager] SwitchLevel: pivot reset to saved euler={savedRot.eulerAngles:F1} for {zone}.");
        }

        _ringState.Remove(zone);
        if (_sessionOrbsByZone.TryGetValue(zone, out var realOrbs) && realOrbs.Count > 0)
            InitialiseRing(zone);
        else
            InitialiseDummyRing(zone);

        // ── Update orbit ring alpha to reflect active/faded state ─────────────
        UpdateRingAlphasForZone(zone);
    }

    /// <summary>
    /// Updates the alpha of all three orbit rings for a zone to reflect
    /// which band is currently active (full alpha) vs faded.
    /// </summary>
    private void UpdateRingAlphasForZone(PhobiaZone zone)
    {
        string activeBand = ActiveBandName(zone);
        OrbLayoutConfig cfg = GetConfigForZone(zone);
        float activeAlpha  = cfg != null ? cfg.ringAlphaActive              : ringAlphaActive;
        float fadedAlpha   = cfg != null ? cfg.ringAlphaFaded               : ringAlphaFaded;
        float baseWidth    = cfg != null ? cfg.ringLineWidth                 : ringLineWidth;
        float widthMult    = cfg != null ? cfg.activeRingLineWidthMultiplier : 1f;

        // Per-band width overrides (use base if override <= 0.001)
        float eqWidth  = cfg != null && cfg.ringLineWidthEquator > 0.001f ? cfg.ringLineWidthEquator : baseWidth;
        float upWidth  = cfg != null && cfg.ringLineWidthUpper   > 0.001f ? cfg.ringLineWidthUpper   : baseWidth;
        float loWidth  = cfg != null && cfg.ringLineWidthLower   > 0.001f ? cfg.ringLineWidthLower   : baseWidth;

        string[] ringNames  = { "Ring_Equator", "Ring_Upper", "Ring_Lower" };
        string[] bands      = { "Equator",      "Upper",      "Lower"      };
        float[]  bandWidths = { eqWidth,         upWidth,      loWidth      };

        for (int i = 0; i < ringNames.Length; i++)
        {
            string key = $"{zone}_{ringNames[i]}";
            if (!_liveRings.TryGetValue(key, out var entry)) continue;

            bool isActive  = bands[i] == activeBand;
            float alpha    = isActive ? activeAlpha : fadedAlpha;
            float width    = isActive ? bandWidths[i] * widthMult : bandWidths[i];

            LineRenderer lr = entry.lr;
            if (lr == null) continue;

            Color c = lr.startColor;
            c.a = alpha;
            lr.startColor = c;
            lr.endColor   = c;
            lr.startWidth = width;
            lr.endWidth   = width;

            _liveRings[key] = (lr, isActive, zone, entry.orbitRadius, entry.latitudeDeg, entry.longitudeOffsetDeg, entry.pivotLocalPos);

            Debug.Log($"[ConstellationManager] UpdateRingAlphasForZone: {zone} {ringNames[i]} " +
                      $"active={isActive} alpha={alpha:F2} width={width:F4}.");
        }
    }

    /// <summary>Returns the band name for the current active level of a zone.</summary>
    private string ActiveBandName(PhobiaZone zone)
    {
        int v = _activeBandByZone.TryGetValue(zone, out int val) ? val : 0;
        return v == 0 ? "Equator" : v > 0 ? "Upper" : "Lower";
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RefreshAllOrbs() { foreach (var orb in _allOrbs) orb.RefreshState(); UpdateCrossoverConnectors(); }

    public void NavigateToVestibularWithBookmark(SessionData bookmark)
    { Debug.Log($"[ConstellationManager] NavigateToVestibularWithBookmark: {bookmark?.sessionID ?? "null"} (post-MVP stub)."); }

    public void ReturnToConstellationWithZoneExpanded(PhobiaZone zone)
    { if (_sessionOrbsByZone.ContainsKey(zone)) ExpandZone(zone); }

    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] OnSessionSelected: {session?.sessionID}");
        SessionHandoff.Set(session);
        SceneManager.LoadScene("Video");
    }

    // ── Startup camera ────────────────────────────────────────────────────────

    private void FaceStartZone()
    {
        Transform target = null;
        if (PhobiaPriorityManager.Instance != null) target = GetClusterRoot(PhobiaPriorityManager.Instance.GetStartZone());
        if (target == null) foreach (var e in zoneClusterEntries) if (e.clusterRoot != null) { target = e.clusterRoot; break; }
        if (target == null) return;
        if (cameraFaceDuration <= 0f) SnapCameraToFace(target.position);
        else StartCoroutine(SmoothFaceTarget(target.position, cameraFaceDuration));
    }

    private void SnapCameraToFace(Vector3 p)
    {
        if (Camera.main == null) return;
        Vector3 dir = p - Camera.main.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Transform rig = Camera.main.transform.parent ?? Camera.main.transform;
        rig.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private IEnumerator SmoothFaceTarget(Vector3 p, float duration)
    {
        if (Camera.main == null) yield break;
        Transform rig = Camera.main.transform.parent ?? Camera.main.transform;
        Quaternion startQ = rig.rotation;
        Vector3 dir = p - Camera.main.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;
        Quaternion endQ = Quaternion.LookRotation(dir.normalized, Vector3.up);
        float elapsed = 0f;
        while (elapsed < duration)
        { elapsed += Time.deltaTime; rig.rotation = Quaternion.Slerp(startQ, endQ, elapsed / duration); yield return null; }
        rig.rotation = endQ;
    }

    private Transform GetClusterRoot(PhobiaZone zone)
    { foreach (var e in zoneClusterEntries) if (e.zone == zone && e.clusterRoot != null) return e.clusterRoot; return null; }

    // ── Label spawn ───────────────────────────────────────────────────────────

    private ZoneLabelController SpawnLabel(Transform parent, string text, string goName, bool parentIsActive)
    {
        if (labelPrefab == null) return null;
        GameObject go = Instantiate(labelPrefab, parent.position + Vector3.up * labelOffset, Quaternion.identity, parent);
        go.name = goName;
        ZoneLabelController label = go.GetComponent<ZoneLabelController>();
        if (label != null) { label.SetLabel(text); if (parentIsActive) label.Show(); else label.SetVisibleImmediate(false); }
        else Debug.LogWarning($"[ConstellationManager] labelPrefab missing ZoneLabelController on '{goName}'.");
        return label;
    }

    // ── Crossover connectors ──────────────────────────────────────────────────

    private void UpdateCrossoverConnectors()
    {
        if (UserProgressService.Instance == null) return;
        if (flyingHeightsConnector != null)
        {
            bool on = SessionRegistry.Instance.GetCrossovers(PhobiaZone.Heights).Count > 0
                      && HasCompletedZone(PhobiaZone.Heights, 2);
            flyingHeightsConnector.gameObject.SetActive(on);
        }
        if (waterSharksConnector != null)
            waterSharksConnector.gameObject.SetActive(HasCompletedZone(PhobiaZone.Water, 2));
    }

    private bool HasCompletedZone(PhobiaZone zone, int min)
    {
        int n = 0;
        foreach (var s in SessionRegistry.Instance.GetByPhobiaZone(zone))
            if (UserProgressService.Instance.IsCompleted(s.sessionID)) n++;
        return n >= min;
    }

    // ── Unused legacy ─────────────────────────────────────────────────────────
    private Vector3 OrbitalPosition(int index, int total, float orbitRadius)
    {
        float angle = (360f / Mathf.Max(total, 1)) * index;
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius, 0f,
                           Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius);
    }
}
