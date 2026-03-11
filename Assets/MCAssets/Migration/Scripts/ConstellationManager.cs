// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:  3.0
// TIMESTAMP: 2026-03-09T00:00:00Z
//
// CHANGE LOG:
//   v3.0  2026-03-09  Zone cluster roots refactored from individual named fields
//                     to a serialised List<ZoneClusterEntry> — add new zones in
//                     Inspector without any code changes. All 12 active zones
//                     pre-populated as default entries.
//                     Singleton Instance added.
//                     Null-guard per session in SpawnZone.
//                     MockUserProgress → UserProgressService in crossover connectors.
//   v2.0  2026-03-09  MockUserProgress → UserProgressService swap (S4.3).
//   v1.0  2026-02-25  Initial implementation.
//
// OBSOLETE FILES: None — same filename, version tracked in header.
//
// ACTIVE ZONES (12):
//   Flying, Heights, Water, Sharks, Crowds, ClosedSpaces, Mountains,
//   Vestibular, OpenSpaces, Mindfulness, Insects, FoodContamination
//
// ADDING A NEW ZONE:
//   1. Append the new value to PhobiaZone enum in SessionData.cs (never insert mid-enum)
//   2. Create an empty GameObject in the Hierarchy under ClusterRoots
//   3. In ConstellationManager Inspector → Zone Cluster Roots → click + → set Zone + Transform

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ZoneClusterEntry
{
    [Tooltip("PhobiaZone enum value this cluster represents")]
    public PhobiaZone zone;

    [Tooltip("Empty GameObject in the Hierarchy that acts as the cluster centre point")]
    public Transform clusterRoot;
}

public class ConstellationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConstellationManager Instance { get; private set; }

    // ── Orb Prefab ────────────────────────────────────────────────────────────
    [Header("Orb Prefab")]
    [Tooltip("Prefab with ConstellationOrb component attached")]
    public GameObject orbPrefab;

    // ── Zone Cluster Roots ────────────────────────────────────────────────────
    [Header("Zone Cluster Roots")]
    [Tooltip("One entry per active zone. Add a new entry here when a new zone is added — no code changes needed.")]
    public List<ZoneClusterEntry> zoneClusterRoots = new List<ZoneClusterEntry>();

    // ── Crossover Connector Lines ─────────────────────────────────────────────
    [Header("Crossover Connector Lines")]
    [Tooltip("LineRenderers connecting crossover zone pairs — hidden until unlocked")]
    public LineRenderer flyingHeightsConnector;
    public LineRenderer waterSharksConnector;

    // ── Spawn Settings ────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Spread radius within each cluster")]
    public float clusterSpread = 1.2f;

    // ── Mindfulness Trigger ───────────────────────────────────────────────────
    [Header("Mindfulness Trigger")]
    [SerializeField] private int mindfulnessAnxietyThreshold = 6;
    [SerializeField] private int mindfulnessRepeatThreshold  = 2;
    [SerializeField] private SessionData mindfulnessSessionOverride;

    // ── Private ───────────────────────────────────────────────────────────────
    private List<ConstellationOrb> _allOrbs = new List<ConstellationOrb>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(BuildConstellation());
    }

    // ── Build ─────────────────────────────────────────────────────────────────
    private IEnumerator BuildConstellation()
    {
        yield return null; // wait one frame for SessionRegistry to initialise

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found. Is it in the scene?");
            yield break;
        }

        foreach (ZoneClusterEntry entry in zoneClusterRoots)
        {
            SpawnZone(entry.zone, entry.clusterRoot);
        }

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built constellation with {_allOrbs.Count} orbs.");
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No cluster root assigned for {zone} — skipping.");
            return;
        }

        List<SessionData> sessions = SessionRegistry.Instance.GetByPhobiaZone(zone);

        if (sessions == null || sessions.Count == 0)
        {
            Debug.Log($"[ConstellationManager] No sessions found for {zone} — skipping.");
            return;
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            SessionData session = sessions[i];

            if (session == null)
            {
                Debug.LogWarning($"[ConstellationManager] Null SessionData in {zone} list at index {i} — skipping.");
                continue;
            }

            Vector3 spawnPos = clusterRoot.position + AutoPosition(i, sessions.Count, session.level);

            GameObject orbGO = Instantiate(orbPrefab, spawnPos, Quaternion.identity, clusterRoot);
            orbGO.name = session.sessionID;

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

    // ── Auto-position ─────────────────────────────────────────────────────────
    private Vector3 AutoPosition(int index, int total, int level)
    {
        float y     = (level - 3) * 0.6f;
        float angle = (index / (float)Mathf.Max(total - 1, 1)) * 120f - 60f;
        float rad   = angle * Mathf.Deg2Rad;
        float x     = Mathf.Sin(rad) * clusterSpread;
        float z     = Mathf.Cos(rad) * clusterSpread * 0.5f;
        return new Vector3(x, y, z);
    }

    // ── Session selected ──────────────────────────────────────────────────────
    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] Session selected: {session.displayTitle}");
        // TODO Sprint 4: SessionLauncher.Instance.Launch(session);
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

    // ── Mindfulness trigger ───────────────────────────────────────────────────
    public void CheckMindfulnessTrigger(int anxietyRating, int repeatCount, PhobiaZone zone)
    {
        if (anxietyRating < mindfulnessAnxietyThreshold) return;
        if (repeatCount   < mindfulnessRepeatThreshold)  return;

        var pool = SessionRegistry.Instance?.GetByPhobiaZone(PhobiaZone.Mindfulness);
        if (pool == null || pool.Count == 0) return;

        var mindfulSession = mindfulnessSessionOverride ?? pool[Random.Range(0, pool.Count)];
        Debug.Log($"[ConstellationManager] Mindfulness trigger fired for {zone} — offering {mindfulSession.sessionID}");
        // TODO: surface Baku offer UI
    }

    // ── Public ────────────────────────────────────────────────────────────────
    public void RefreshAllOrbs()
    {
        foreach (var orb in _allOrbs)
            orb.RefreshState();
        UpdateCrossoverConnectors();
    }
}
