// ═══════════════════════════════════════════════════════════════════════════
// ConstellationManager.cs
// Assets/MCAssets/Migration/Scripts/ConstellationManager.cs
//
// VERSION:  v2                          DATE: 2026-03-07
// TIMESTAMP: 2026-03-07T12:00:00Z
//
// CHANGE LOG:
//   v2  2026-03-07  ALL 12 ZONES WIRED
//     - v1 only declared 5 cluster root slots (Flying, Heights, Water, Sharks,
//       Crowds) and only called SpawnZone() for those 5.
//     - Added Inspector slots for all remaining zones:
//         closedSpacesClusterRoot, mountainsClusterRoot, vestibularClusterRoot,
//         openSpacesClusterRoot, mindfulnessClusterRoot, insectsClusterRoot,
//         foodContaminationClusterRoot
//     - BuildConstellation() now calls SpawnZone() for all 12 active zones.
//     - Mindfulness note added: spawns via SpawnZone like all others, but orbs
//       are contextually triggered — ConstellationOrb will show/hide based on
//       anxiety score. The slot must still be assigned in Inspector.
//     - Vestibular note added: slot must be assigned; zone gates all others until
//       VR onboarding is complete (handled by UserProgressService in Sprint 6;
//       MockUserProgress returns Locked for all non-Vestibular zones until then).
//     - No changes to SpawnZone(), AutoPosition(), crossover logic, or public API.
//     - OBSOLETE: ConstellationManager.cs (v1, 2026-03-07)
//
//   v1  2026-03-07  Initial creation. 5 zones only (incomplete).
//
// ZONE CLUSTER ROOTS — assign all 12 in Inspector:
//   Flying, Heights, Water, Sharks, Crowds, ClosedSpaces, Mountains,
//   Vestibular, OpenSpaces, Mindfulness, Insects, FoodContamination
//
// NOTE — Mindfulness:
//   Spawns in the constellation like other zones. Individual orbs are shown or
//   hidden contextually (post-session anxiety trigger). Assign a cluster root.
//
// NOTE — Vestibular:
//   Acts as the gate for all other zones. Until the first Vestibular session is
//   completed, all other zone orbs display as Locked. This is enforced by the
//   Vestibular gate system (Sprint 6 / UserProgressService), not here.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ConstellationManager — spawns and arranges all orbs in the constellation scene.
/// Reads from SessionRegistry for content, MockUserProgress for visual states.
/// Swap MockUserProgress for the real Supabase backend when ready — nothing else changes.
///
/// Attach to a GameObject in your Constellation scene.
/// </summary>
public class ConstellationManager : MonoBehaviour
{
    [Header("Orb Prefab")]
    [Tooltip("Prefab with ConstellationOrb component attached")]
    public GameObject orbPrefab;

    // ── Zone Cluster Roots ────────────────────────────────────────────────────
    // One empty GameObject per zone — defines the centre of each cluster.
    // ALL 12 must be assigned in the Inspector.

    [Header("Zone Cluster Roots — assign all 12")]
    [Tooltip("Centre point of the Flying zone cluster")]
    public Transform flyingClusterRoot;

    [Tooltip("Centre point of the Heights zone cluster")]
    public Transform heightsClusterRoot;

    [Tooltip("Centre point of the Water zone cluster")]
    public Transform waterClusterRoot;

    [Tooltip("Centre point of the Sharks zone cluster")]
    public Transform sharksClusterRoot;

    [Tooltip("Centre point of the Crowds zone cluster")]
    public Transform crowdsClusterRoot;

    [Tooltip("Centre point of the ClosedSpaces zone cluster (was Claustrophobia)")]
    public Transform closedSpacesClusterRoot;

    [Tooltip("Centre point of the Mountains zone cluster")]
    public Transform mountainsClusterRoot;

    [Tooltip("Centre point of the Vestibular zone cluster. " +
             "Gates all other zones until VR onboarding is complete.")]
    public Transform vestibularClusterRoot;

    [Tooltip("Centre point of the OpenSpaces zone cluster (Agoraphobia)")]
    public Transform openSpacesClusterRoot;

    [Tooltip("Centre point of the Mindfulness zone cluster. " +
             "Orbs are contextually shown/hidden — cluster root still required.")]
    public Transform mindfulnessClusterRoot;

    [Tooltip("Centre point of the Insects zone cluster (Entomophobia)")]
    public Transform insectsClusterRoot;

    [Tooltip("Centre point of the FoodContamination zone cluster (Mysophobia)")]
    public Transform foodContaminationClusterRoot;

    // ── Crossover Connector Lines ─────────────────────────────────────────────
    [Header("Crossover Connector Lines")]
    [Tooltip("LineRenderer connecting Flying ↔ Heights — hidden until both zones have completions")]
    public LineRenderer flyingHeightsConnector;

    [Tooltip("LineRenderer connecting Water ↔ Sharks — hidden until both zones have completions")]
    public LineRenderer waterSharksConnector;

    // ── Spawn Settings ────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Spread radius within each cluster")]
    public float clusterSpread = 1.2f;

    // ── Private ───────────────────────────────────────────────────────────────
    private List<ConstellationOrb> _allOrbs = new List<ConstellationOrb>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        StartCoroutine(BuildConstellation());
    }

    private IEnumerator BuildConstellation()
    {
        // Wait one frame for SessionRegistry and MockUserProgress to initialise
        yield return null;

        if (SessionRegistry.Instance == null)
        {
            Debug.LogError("[ConstellationManager] SessionRegistry not found. Is it in the scene?");
            yield break;
        }

        // All 12 active PhobiaZones — matches PhobiaZone enum in SessionData.cs
        // Mindfulness spawns here; contextual show/hide is handled per-orb elsewhere.
        // Vestibular gates all others; locking is enforced by UserProgressService (Sprint 6).
        SpawnZone(PhobiaZone.Flying,             flyingClusterRoot);
        SpawnZone(PhobiaZone.Heights,            heightsClusterRoot);
        SpawnZone(PhobiaZone.Water,              waterClusterRoot);
        SpawnZone(PhobiaZone.Sharks,             sharksClusterRoot);
        SpawnZone(PhobiaZone.Crowds,             crowdsClusterRoot);
        SpawnZone(PhobiaZone.ClosedSpaces,       closedSpacesClusterRoot);
        SpawnZone(PhobiaZone.Mountains,          mountainsClusterRoot);
        SpawnZone(PhobiaZone.Vestibular,         vestibularClusterRoot);
        SpawnZone(PhobiaZone.OpenSpaces,         openSpacesClusterRoot);
        SpawnZone(PhobiaZone.Mindfulness,        mindfulnessClusterRoot);
        SpawnZone(PhobiaZone.Insects,            insectsClusterRoot);
        SpawnZone(PhobiaZone.FoodContamination,  foodContaminationClusterRoot);

        UpdateCrossoverConnectors();

        Debug.Log($"[ConstellationManager] Built constellation with {_allOrbs.Count} orbs.");
    }

    // ── Zone spawning ─────────────────────────────────────────────────────────

    private void SpawnZone(PhobiaZone zone, Transform clusterRoot)
    {
        if (clusterRoot == null)
        {
            Debug.LogWarning($"[ConstellationManager] No cluster root assigned for {zone} — skipping.");
            return;
        }

        List<SessionData> sessions = SessionRegistry.Instance.GetByPhobiaZone(zone);

        if (sessions.Count == 0)
        {
            Debug.Log($"[ConstellationManager] No sessions found for {zone} — cluster root exists but spawned nothing.");
            return;
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            SessionData session = sessions[i];

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

    /// <summary>Auto-position orbs within a cluster based on index and level.</summary>
    private Vector3 AutoPosition(int index, int total, int level)
    {
        // Vertical: higher level = higher position. Level 3 = eye level.
        float y = (level - 3) * 0.6f;

        // Horizontal: spread orbs in a small arc
        float angle = (index / (float)Mathf.Max(total - 1, 1)) * 120f - 60f;
        float rad   = angle * Mathf.Deg2Rad;
        float x     = Mathf.Sin(rad) * clusterSpread;
        float z     = Mathf.Cos(rad) * clusterSpread * 0.5f;

        return new Vector3(x, y, z);
    }

    private void OnSessionSelected(SessionData session)
    {
        Debug.Log($"[ConstellationManager] Session selected: {session.displayTitle}");
        // TODO Sprint 4: fade to antechamber or 360 video
        // SessionLauncher.Instance.Launch(session);
    }

    // ── Crossover connectors ──────────────────────────────────────────────────

    private void UpdateCrossoverConnectors()
    {
        if (MockUserProgress.Instance == null) return;

        // Flying ↔ Heights: visible once the user has completions in both zones
        if (flyingHeightsConnector != null)
        {
            bool unlocked = HasCompletedZone(PhobiaZone.Flying, 1)
                         && HasCompletedZone(PhobiaZone.Heights, 1);
            flyingHeightsConnector.gameObject.SetActive(unlocked);
        }

        // Water ↔ Sharks: visible once the user has completions in both zones
        if (waterSharksConnector != null)
        {
            bool unlocked = HasCompletedZone(PhobiaZone.Water, 1)
                         && HasCompletedZone(PhobiaZone.Sharks, 1);
            waterSharksConnector.gameObject.SetActive(unlocked);
        }
    }

    private bool HasCompletedZone(PhobiaZone zone, int minCompletions)
    {
        int count = 0;
        foreach (var s in SessionRegistry.Instance.GetByPhobiaZone(zone))
            if (MockUserProgress.Instance.IsCompleted(s.sessionID)) count++;
        return count >= minCompletions;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>Call this after any progress change to refresh all orb visuals.</summary>
    public void RefreshAllOrbs()
    {
        foreach (var orb in _allOrbs)
            orb.RefreshState();
        UpdateCrossoverConnectors();
    }
}
