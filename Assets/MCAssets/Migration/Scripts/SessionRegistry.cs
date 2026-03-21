// SessionRegistry.cs
// Assets/MCAssets/Migration/Scripts/SessionRegistry.cs
//
// VERSION:  3
// DATE:     2026-03-21
// TIMESTAMP: 2026-03-21T22:30:00Z
//
// PURPOSE:
//   Singleton ScriptableObject-collection holder. Loaded at runtime by
//   SessionScanner (Editor) or manually dragged into the Inspector slot.
//   All other scripts read sessions through this — never directly from
//   Resources or AssetDatabase.
//
// PUBLIC API:
//   allSessions                    — full flat list
//   GetByPhobiaZone(zone)          — sessions for a given PhobiaZone
//   GetSessionsByZone(zone)        — alias of GetByPhobiaZone
//   GetSession(sessionID)          — lookup by string ID
//   GetCrossovers(zone)            — crossover sessions that include this zone
//   GetMindfulnessPool()           — isMindfulnessSession sessions (all levels)
//   GetVestibularOnboardingPool()  — isOnboardingEligible sessions only
//   GetVestibularRecoveryPool()    — isRecoverySession sessions only
//
// CHANGE LOG:
//   v3  2026-03-21  REMOVE DontDestroyOnLoad
//     - DontDestroyOnLoad removed from Awake().
//     - Root cause of GameManager disappearing during Play: SessionRegistry
//       called DontDestroyOnLoad(gameObject), moving GameManager to the
//       DontDestroyOnLoad group. On a second Play press, the survivor from
//       the previous session triggered the duplicate check and destroyed the
//       new GameManager instance — taking all components with it.
//     - SessionRegistry is a single-scene object for MVP. It does not need
//       to persist across scene loads.
//     - Duplicate check changed to Destroy(this) instead of Destroy(gameObject)
//       to avoid destroying the entire GameManager if a duplicate component
//       is added by mistake.
//   v2  2026-03-15  GetMindfulnessPool() added.
//   v1  2026-03-07  Initial creation.
//
// OBSOLETE FILES:
//   SessionRegistry.cs v2 (2026-03-15)
//   SessionRegistry.cs v1 (2026-03-07)
//
// FILE LOCATION:
//   Assets/MCAssets/Migration/Scripts/SessionRegistry.cs
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton that holds all SessionData ScriptableObjects loaded by SessionScanner.
/// Attach to the GameManager GameObject alongside MockUserProgress.
/// SessionScanner (Editor tool) populates allSessions from the CSV — do not edit manually.
/// </summary>
public class SessionRegistry : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static SessionRegistry Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SessionRegistry] Duplicate instance detected — destroying component.");
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("[SessionRegistry] Awake — singleton set.");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[SessionRegistry] OnDestroy — singleton cleared.");
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// All sessions loaded from the CSV. Populated by SessionScanner (Editor).
    /// Do not modify at runtime.
    /// </summary>
    [Tooltip("Populated by SessionScanner — MasterChange > Import Sessions from CSV")]
    public List<SessionData> allSessions = new List<SessionData>();

    // ── Primary lookups ───────────────────────────────────────────────────────

    /// <summary>
    /// All sessions whose primaryZone matches the given zone.
    /// Used by ConstellationManager to populate each cluster.
    /// </summary>
    public List<SessionData> GetByPhobiaZone(PhobiaZone zone)
    {
        return allSessions
            .Where(s => s.primaryZone == zone)
            .OrderBy(s => s.level)
            .ToList();
    }

    /// <summary>
    /// Alias for GetByPhobiaZone — used in Sprint 4+ guide code (RirosManager, etc.).
    /// </summary>
    public List<SessionData> GetSessionsByZone(PhobiaZone zone)
        => GetByPhobiaZone(zone);

    /// <summary>
    /// Look up a single session by its sessionID string.
    /// Returns null if not found — callers must null-check.
    /// </summary>
    public SessionData GetSession(string sessionID)
    {
        return allSessions.FirstOrDefault(s => s.sessionID == sessionID);
    }

    // ── Crossover lookups ─────────────────────────────────────────────────────

    /// <summary>
    /// Sessions that are crossover sessions AND include the given zone as
    /// either their primaryZone or in additionalZones.
    /// Used by ConstellationManager.UpdateCrossoverConnectors().
    /// </summary>
    public List<SessionData> GetCrossovers(PhobiaZone zone)
    {
        return allSessions
            .Where(s => s.isCrossover &&
                        (s.primaryZone == zone || s.additionalZones.Contains(zone)))
            .ToList();
    }

    // ── Mindfulness pool ──────────────────────────────────────────────────────

    /// <summary>
    /// All sessions flagged as Mindfulness sessions (isMindfulnessSession = true).
    /// Used by PostSessionController.CheckMindfulnessTrigger().
    /// </summary>
    public List<SessionData> GetMindfulnessPool()
    {
        return allSessions
            .Where(s => s.isMindfulnessSession)
            .OrderBy(s => s.level)
            .ToList();
    }

    // ── Vestibular pools ──────────────────────────────────────────────────────

    /// <summary>
    /// Sessions eligible as the first Vestibular onboarding experience.
    /// </summary>
    public List<SessionData> GetVestibularOnboardingPool()
    {
        return allSessions
            .Where(s => s.primaryZone == PhobiaZone.Vestibular && s.isOnboardingEligible)
            .OrderBy(s => s.level)
            .ToList();
    }

    /// <summary>
    /// Shorter recovery sessions used between intense phobia sessions.
    /// </summary>
    public List<SessionData> GetVestibularRecoveryPool()
    {
        return allSessions
            .Where(s => s.primaryZone == PhobiaZone.Vestibular && s.isRecoverySession)
            .OrderBy(s => s.level)
            .ToList();
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    [ContextMenu("Log Session Count")]
    public void LogSessionCount()
    {
        Debug.Log($"[SessionRegistry] {allSessions.Count} sessions loaded.");
        foreach (PhobiaZone zone in System.Enum.GetValues(typeof(PhobiaZone)))
        {
            int count = GetByPhobiaZone(zone).Count;
            if (count > 0)
                Debug.Log($"  {zone}: {count} session(s)");
        }
    }
}
