// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:   3.23
// DATE:      2026-04-06
// TIMESTAMP: 2026-04-07T00:00:00Z
//
// CHANGE LOG:
//   v3.23 2026-04-07  FIX — PER-ZONE SLOT LIST ISOLATION
//     - SpawnZone: slot lists (equatorSlots/upperSlots/lowerSlots) now read from
//       the zone's own config asset as fresh local copies, not from the manager's
//       shared fields. This fixes orb misalignment where Zone 2+ inherited Zone 1's
//       slot lat values because the shared list was non-empty and EnsureSlotDefaults
//       was skipped. Each zone now gets independent lists every spawn.
//     - When cfg is null (no config assigned), manager fields are copied (not
//       referenced) so EnsureSlotDefaults cannot mutate the manager's shared list.
//     - RebuildDummyOrbs: before calling SpawnZone, copies non-empty manager slot
//       lists into the zone's config. This preserves the in-Play editor tuning path
//       where OrbLayoutEditor writes to mgr.equatorSlots before triggering rebuild.
//
//   v3.22 2026-04-06  PER-BAND ORBIT RADIUS
//     - SpawnZone now resolves three separate orbit radius values, one per band:
//         eqOrbitRadius  = colliderRadius × equatorRadiusMultiplier + padding
//         upOrbitRadius  = colliderRadius × upperRadiusMultiplier   + padding
//         loOrbitRadius  = colliderRadius × lowerRadiusMultiplier   + padding
//     - Each SpawnSlotBand and SpawnOrbitRing call receives its own radius.
//     - Fallback when cfg is null: all three use manager.orbitalRadiusMultiplier
//       (global default retained for backward compat when no config assigned).
//     - Global default field orbitalRadiusMultiplier retained on manager.
//     - OrbLayoutConfig v3.2: equatorRadiusMultiplier / upperRadiusMultiplier /
//       lowerRadiusMultiplier replace single orbitalRadiusMultiplier.
//
//   v3.21 2026-04-06  PER-ZONE CONFIG
//     - ZoneClusterEntry: added `public OrbLayoutConfig layoutConfig` field.
//       Each zone now carries its own config asset in the Inspector. All orb
//       geometry for that zone is driven exclusively by that config when assigned.
//     - Removed top-level `public OrbLayoutConfig layoutConfig` from
//       ConstellationManager. The manager-level Inspector fields (orbSize, latitudes,
//       colours etc.) now serve as global defaults only — used when a zone's
//       entry.layoutConfig is null.
//     - SpawnZone(PhobiaZone, Transform, OrbLayoutConfig): config parameter added.
//       All geometry values (orbSize, latitudes, slots, radius, colours, ring,
//       chevron, pivot euler) are read from the passed config. Falls back to
//       manager Inspector fields when config is null.
//     - BuildConstellation: passes entry.layoutConfig to SpawnZone per entry.
//     - RebuildDummyOrbs(PhobiaZone): looks up the zone's entry.layoutConfig and
//       passes it to SpawnZone. This is why orb size now updates on rebuild — the
//       correct per-zone config is used, not shared manager fields.
//     - EnsureSlotDefaults(OrbLayoutConfig cfg): takes config parameter; reads
//       bandOrbCount and lat values from cfg (or manager defaults if null).
//     - ApplyLiveProperties(PhobiaZone, OrbLayoutConfig): zone-scoped. Only
//       updates _liveRings and _liveChevrons that belong to the specified zone.
//       Keyed by zone prefix in ring name (e.g. "Flying_Ring_Equator").
//     - Ring/chevron names now prefixed with zone: "{zone}_Ring_Equator" etc.
//       This allows per-zone live updates without touching other zones' renderers.
//     - _liveRings key changed to "{zone}_{ringName}" format.
//     - SetOrbPivotRotation(PhobiaZone, Quaternion): unchanged, already zone-scoped.
//     - SavePlanetTransformToConfig / ApplyPlanetTransformFromConfig: unchanged,
//       already take explicit cfg parameter.
//     - SetZoneVisible: unchanged.
//     - GetConfigForZone(PhobiaZone): new helper returns entry.layoutConfig or null.
//
//   v3.20 2026-04-06  ORB PIVOT + MULTI-PLANET PREVIEW + PLANET TRANSFORM
//   v3.19 2026-04-06  LIVE LAYOUT EDITOR SUPPORT
//   v3.18 2026-04-05  PHASE 1 VISUAL COMPLETE
//   v3.17–v3.12 see prior headers
//   v3.11–v1    see prior headers
//
// OBSOLETE — DELETE:
//   ConstellationManager.cs v3.23 supersedes v3.22
//   ConstellationManager.cs v3.22 supersedes v3.21
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
//   ConstellationOrb.cs v4.1       SetTier() [Phase 2+]
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
        public int frontIndex;
        public OrbRingState(List<ConstellationOrb> orbs, int front)
        { orderedOrbs = orbs; frontIndex = front; }
    }

    // ── Live-update targets ───────────────────────────────────────────────────
    // Key: "{zone}_{ringName}" e.g. "Flying_Ring_Equator"
    private Dictionary<string, (LineRenderer lr, bool isActive, PhobiaZone zone)> _liveRings
        = new Dictionary<string, (LineRenderer, bool, PhobiaZone)>();

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
        // Slot lists: read from the zone's own config asset. Each zone gets fresh
        // local lists so zones never share or corrupt each other's slot data.
        // Manager-level equatorSlots/upperSlots/lowerSlots are only used as a
        // fallback when no config is assigned, and are copied (not referenced) so
        // the manager's list is not mutated by EnsureSlotDefaults.
        List<Vector2> eqSlots = cfg != null
            ? new List<Vector2>(cfg.equatorSlots)
            : new List<Vector2>(equatorSlots);
        List<Vector2> upSlots = cfg != null
            ? new List<Vector2>(cfg.upperSlots)
            : new List<Vector2>(upperSlots);
        List<Vector2> loSlots = cfg != null
            ? new List<Vector2>(cfg.lowerSlots)
            : new List<Vector2>(lowerSlots);
        int    orbCount        = cfg != null ? cfg.bandOrbCount            : bandOrbCount;
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
        // Per-band radius: each ring has an independent multiplier for visual variety
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
        EnsureSlotDefaults(eqSlots, upSlots, loSlots, orbCount, eqLat, upLat, loLat, zone);

        // ── Camera-facing longitude offset ────────────────────────────────────
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
        pivotGO.transform.position = planetWorldCentre;
        pivotGO.transform.rotation = Quaternion.Euler(pivotEuler);
        Transform pivot = pivotGO.transform;
        _orbPivots[zone] = pivot;

        Debug.Log($"[ConstellationManager] OrbPivot_{zone} at {planetWorldCentre:F3} " +
                  $"euler={pivotEuler:F1}.");

        // ── Materials ─────────────────────────────────────────────────────────
        Shader   urpUnlit  = Shader.Find("Universal Render Pipeline/Unlit");
        Material matEquator = MakeBandMaterial(urpUnlit, cCurrent, "Equator");
        Material matUpper   = MakeBandMaterial(urpUnlit, cNext,    "Upper");
        Material matLower   = MakeBandMaterial(urpUnlit, cPrev,    "Lower");
        Material matRing    = MakeRingMaterial(urpUnlit, rColour);

        // ── Spawn bands ───────────────────────────────────────────────────────
        List<GameObject> allDummy = new List<GameObject>();
        allDummy.Add(pivotGO);

        List<GameObject> equatorOrbs = SpawnSlotBand(zone, pivot, eqOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            eqLat, eqSlots, longitudeOffsetDeg, matEquator, "Equator", ref allDummy);

        SpawnSlotBand(zone, pivot, upOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            upLat, upSlots, longitudeOffsetDeg, matUpper, "Upper", ref allDummy);

        SpawnSlotBand(zone, pivot, loOrbitRadius,
            planetVisualDiameter, orbSize, frontScale, sideScale,
            loLat, loSlots, longitudeOffsetDeg, matLower, "Lower", ref allDummy);

        // ── Spawn rings ───────────────────────────────────────────────────────
        SpawnOrbitRing(zone, pivot, eqOrbitRadius, eqLat, rAlphaActive,
                       matRing, rColour, rWidth, rSegments, "Ring_Equator", isActive: true, ref allDummy);
        SpawnOrbitRing(zone, pivot, upOrbitRadius, upLat, rAlphaFaded,
                       matRing, rColour, rWidth, rSegments, "Ring_Upper", isActive: false, ref allDummy);
        SpawnOrbitRing(zone, pivot, loOrbitRadius, loLat, rAlphaFaded,
                       matRing, rColour, rWidth, rSegments, "Ring_Lower", isActive: false, ref allDummy);

        // ── Chevrons ──────────────────────────────────────────────────────────
        if (equatorOrbs.Count >= 3)
        {
            SpawnChevron(zone, equatorOrbs[1], +1f, chColour, chWidth, chFraction,
                         "Chevron_Right", ref allDummy);
            SpawnChevron(zone, equatorOrbs[equatorOrbs.Count - 1], -1f, chColour, chWidth, chFraction,
                         "Chevron_Left", ref allDummy);
        }

        _dummyOrbsByZone[zone]   = allDummy;
        _sessionOrbsByZone[zone] = new List<GameObject>();

        foreach (var go in allDummy)
            if (go != null && go != pivotGO) go.SetActive(false);

        Debug.Log($"[ConstellationManager] SpawnZone complete: {zone} — " +
                  $"{allDummy.Count} objects (pivot + orbs + rings + chevrons).");
    }

    // ── Slot defaults ─────────────────────────────────────────────────────────

    private void EnsureSlotDefaults(List<Vector2> eqSlots, List<Vector2> upSlots,
                                    List<Vector2> loSlots, int count,
                                    float eqLat, float upLat, float loLat, PhobiaZone zone)
    {
        if (eqSlots.Count == 0)
        {
            for (int i = 0; i < count; i++)
                eqSlots.Add(new Vector2(eqLat, (360f / count) * i));
            Debug.Log($"[ConstellationManager] {zone} equatorSlots auto-filled: {count} slots.");
        }
        if (upSlots.Count == 0)
        {
            for (int i = 0; i < count; i++)
                upSlots.Add(new Vector2(upLat, (360f / count) * i));
            Debug.Log($"[ConstellationManager] {zone} upperSlots auto-filled: {count} slots.");
        }
        if (loSlots.Count == 0)
        {
            for (int i = 0; i < count; i++)
                loSlots.Add(new Vector2(loLat, (360f / count) * i));
            Debug.Log($"[ConstellationManager] {zone} lowerSlots auto-filled: {count} slots.");
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

    private List<GameObject> SpawnSlotBand(PhobiaZone zone, Transform pivot,
                                           float orbitRadius,
                                           float planetVisualDiameter,
                                           float orbSizePct, float frontSc, float sideSc,
                                           float defaultLatDeg,
                                           List<Vector2> slots, float longitudeOffsetDeg,
                                           Material bandMaterial, string bandName,
                                           ref List<GameObject> collector)
    {
        float baseOrbScale = planetVisualDiameter * (orbSizePct / 100f);
        var spawned = new List<GameObject>();

        for (int i = 0; i < slots.Count; i++)
        {
            float latDeg = defaultLatDeg;
            float lonDeg = slots[i].y + longitudeOffsetDeg;

            if (bandName != "Equator" && Mathf.Abs(slots[i].x) > 0.01f)
                latDeg = slots[i].x;

            Vector3 localOffset = OrbitalPositionOnSphere(orbitRadius, latDeg, lonDeg);

            float perspScale = baseOrbScale;
            if (Camera.main != null)
            {
                Vector3 toCam = (Camera.main.transform.position - pivot.position).normalized;
                float   dot   = Mathf.Abs(Vector3.Dot(localOffset.normalized, toCam));
                perspScale    = baseOrbScale * Mathf.Lerp(sideSc, frontSc, dot);
            }

            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dummy.name = $"P1_{zone}_{bandName}_{i}";
            dummy.transform.SetParent(pivot, worldPositionStays: false);
            dummy.transform.localPosition = localOffset;
            dummy.transform.localScale    = Vector3.one * perspScale;

            Renderer rend = dummy.GetComponent<Renderer>();
            if (rend != null && bandMaterial != null) rend.sharedMaterial = bandMaterial;

            Collider col = dummy.GetComponent<Collider>();
            if (col != null) Destroy(col);

            spawned.Add(dummy);
            collector.Add(dummy);

            Debug.Log($"[ConstellationManager] {zone} {bandName}[{i}]: " +
                      $"lat={latDeg:F1}° lon={lonDeg:F1}° scale={perspScale:F3}.");
        }

        Debug.Log($"[ConstellationManager] {zone} Band {bandName}: {spawned.Count} orbs. " +
                  $"orbSize={orbSizePct:F1}% baseScale={baseOrbScale:F4}.");
        return spawned;
    }

    // ── Orbit ring LineRenderer ───────────────────────────────────────────────

    private void SpawnOrbitRing(PhobiaZone zone, Transform pivot,
                                float orbitRadius, float latitudeDeg, float alpha,
                                Material ringMaterial, Color rColour, float rWidth, int rSegs,
                                string ringName, bool isActive, ref List<GameObject> collector)
    {
        if (ringMaterial == null) return;

        GameObject ringGO = new GameObject(ringName);
        ringGO.transform.SetParent(pivot, worldPositionStays: false);
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

        float latRad = latitudeDeg * Mathf.Deg2Rad;
        float y = orbitRadius * Mathf.Sin(latRad);
        float r = orbitRadius * Mathf.Cos(latRad);

        for (int i = 0; i < rSegs; i++)
        {
            float lonRad = (360f / rSegs) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Sin(lonRad) * r, y, Mathf.Cos(lonRad) * r));
        }

        // Key includes zone prefix so live updates are zone-scoped
        string key = $"{zone}_{ringName}";
        _liveRings[key] = (lr, isActive, zone);

        collector.Add(ringGO);
        Debug.Log($"[ConstellationManager] {zone} Ring {ringName}: lat={latitudeDeg}° alpha={alpha:F2}.");
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
            pivot.rotation = rotation;
            Debug.Log($"[ConstellationManager] SetOrbPivotRotation: {zone} euler={rotation.eulerAngles:F1}.");
        }
        else
            Debug.LogWarning($"[ConstellationManager] SetOrbPivotRotation: no pivot for {zone}.");
    }

    public void SetAllOrbPivotRotation(Quaternion rotation)
    {
        int count = 0;
        foreach (var kvp in _orbPivots)
            if (kvp.Value != null) { kvp.Value.rotation = rotation; count++; }
        Debug.Log($"[ConstellationManager] SetAllOrbPivotRotation: euler={rotation.eulerAngles:F1} × {count} pivots.");
    }

    public Vector3 GetOrbPivotEuler(PhobiaZone zone)
    {
        if (_orbPivots.TryGetValue(zone, out Transform pivot) && pivot != null)
            return pivot.rotation.eulerAngles;
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

    /// <summary>
    /// Applies colour/width changes to rings and chevrons belonging to a specific zone only.
    /// Called by OrbLayoutEditor when the active edit zone's live properties change.
    /// </summary>
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

        // Remove live targets for this zone only
        var keysToRemove = new List<string>();
        foreach (var kvp in _liveRings)
            if (kvp.Value.zone == zone) keysToRemove.Add(kvp.Key);
        foreach (var k in keysToRemove) _liveRings.Remove(k);
        _liveChevrons.RemoveAll(t => t.zone == zone);
        _orbPivots.Remove(zone);

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

            // If the editor populated the manager's slot lists (for in-Play tuning),
            // copy them into the config before spawning so SpawnZone reads them.
            // Only copy when non-empty — empty means "auto-fill from lat values".
            var zoneCfg = entry.layoutConfig;
            if (zoneCfg != null)
            {
                if (equatorSlots.Count > 0) zoneCfg.equatorSlots = new List<Vector2>(equatorSlots);
                if (upperSlots.Count   > 0) zoneCfg.upperSlots   = new List<Vector2>(upperSlots);
                if (lowerSlots.Count   > 0) zoneCfg.lowerSlots   = new List<Vector2>(lowerSlots);
            }
            SpawnZone(zone, entry.clusterRoot, zoneCfg);

            if (_expandedZone == zone && _dummyOrbsByZone.TryGetValue(zone, out var newOrbs))
                foreach (var go in newOrbs) if (go != null) go.SetActive(true);

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

        if (_dummyOrbsByZone.TryGetValue(zone, out List<GameObject> dummies))
        {
            foreach (var go in dummies) if (go != null) go.SetActive(true);
            Debug.Log($"[ConstellationManager] Expanded {zone}: {dummies.Count} objects shown.");
        }

        if (_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> realOrbs) && realOrbs.Count > 0)
            InitialiseRing(zone);

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

        _ringState.Remove(zone);

        if (notifyZonePlanet && _allZonePlanets.TryGetValue(zone, out ZonePlanet zp))
            zp.Collapse();

        if (_expandedZone == zone) _expandedZone = PhobiaZone.None;
        Debug.Log($"[ConstellationManager] Collapsed: {zone}.");
    }

    // ── Ring management (Phase 3+) ────────────────────────────────────────────

    private void InitialiseRing(PhobiaZone zone)
    {
        if (!_sessionOrbsByZone.TryGetValue(zone, out List<GameObject> orbGOs)) return;
        var orbs = new List<ConstellationOrb>();
        foreach (var go in orbGOs) { if (go == null) continue; var o = go.GetComponent<ConstellationOrb>(); if (o != null) orbs.Add(o); }
        if (orbs.Count == 0) { Debug.LogWarning($"[ConstellationManager] InitialiseRing: no orbs for {zone}. (Phase 2+)"); return; }

        int front = 0;
        for (int i = 0; i < orbs.Count; i++)
        {
            string sid = orbs[i].session?.sessionID;
            if (sid != null && !UserProgressService.Instance.IsCompleted(sid)) { front = i; break; }
        }
        _ringState[zone] = new OrbRingState(orbs, front);
        ApplyRingTiers(zone);
        Debug.Log($"[ConstellationManager] Ring initialised: {zone} front={front}/{orbs.Count}.");
    }

    private void ApplyRingTiers(PhobiaZone zone)
    {
        if (!_ringState.TryGetValue(zone, out OrbRingState ring)) return;
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

    public void RotateRing(PhobiaZone zone, int direction)
    {
        if (!_ringState.TryGetValue(zone, out OrbRingState ring))
        { Debug.LogWarning($"[ConstellationManager] RotateRing: no state for {zone}."); return; }
        int count = ring.orderedOrbs.Count; if (count <= 1) return;
        if (_ringTweenCoroutines.TryGetValue(zone, out Coroutine ex) && ex != null) StopCoroutine(ex);
        int newFront = (ring.frontIndex + direction + count) % count;
        ring.frontIndex = newFront;
        float stepAngle = 360f / count * -direction;
        _ringTweenCoroutines[zone] = StartCoroutine(TweenRing(zone, stepAngle));
        Debug.Log($"[ConstellationManager] RotateRing: {zone} dir={direction} front→{newFront}.");
    }

    private IEnumerator TweenRing(PhobiaZone zone, float stepAngle)
    {
        if (!_allZonePlanets.TryGetValue(zone, out ZonePlanet zp)) yield break;
        Transform t = zp.transform;
        Quaternion startRot = t.rotation;
        Quaternion endRot   = Quaternion.AngleAxis(stepAngle, Vector3.up) * startRot;
        float elapsed = 0f;
        while (elapsed < ringTweenDuration)
        {
            elapsed += Time.deltaTime;
            float tt = Mathf.Clamp01(elapsed / ringTweenDuration);
            t.rotation = Quaternion.Slerp(startRot, endRot, tt * tt * (3f - 2f * tt));
            yield return null;
        }
        t.rotation = endRot;
        ApplyRingTiers(zone);
        _ringTweenCoroutines[zone] = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RefreshAllOrbs() { foreach (var orb in _allOrbs) orb.RefreshState(); UpdateCrossoverConnectors(); }

    public void NavigateToVestibularWithBookmark(SessionData bookmark)
    { Debug.Log($"[ConstellationManager] NavigateToVestibularWithBookmark: {bookmark?.sessionID ?? "null"} (post-MVP stub)."); }

    public void ReturnToConstellationWithZoneExpanded(PhobiaZone zone)
    { if (_sessionOrbsByZone.ContainsKey(zone)) ExpandZone(zone); }

    private void OnSessionSelected(SessionData session)
    {
        if (AntechamberController.Instance != null) AntechamberController.Instance.ShowForSession(session);
        else SessionLauncher.Instance?.LaunchSession(session);
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
