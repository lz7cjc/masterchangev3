// SessionRegistry.cs
// MasterChange VR — Sprint 1
// Version   : v1
// Created   : 2026-03-07
// Location  : Assets/MCAssets/Migration/Scripts/SessionRegistry.cs
//
// Purpose   : Singleton ScriptableObject-collection holder. Loaded at runtime by
//             SessionScanner (Editor) or manually dragged into the Inspector slot.
//             All other scripts read sessions through this — never directly from
//             Resources or AssetDatabase.
//
// Public API (consumed by ConstellationManager, MockUserProgress, UserProgressService,
//             RirosManager, HeadsetMonitor):
//   allSessions                          — full flat list
//   GetByPhobiaZone(zone)               — sessions for a given PhobiaZone
//   GetSessionsByZone(zone)             — alias of GetByPhobiaZone (used in guide S4+)
//   GetSession(sessionID)               — lookup by string ID
//   GetCrossovers(zone)                 — crossover sessions that include this zone
//   GetVestibularOnboardingPool()       — isOnboardingEligible sessions only
//   GetVestibularRecoveryPool()         — isRecoverySession sessions only
//
// Change log:
//   v1  2026-03-07  Initial creation. Covers full API surface required by Sprint 1–6.
// ─────────────────────────────────────────────────────────────────────────────────

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
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    // ── Vestibular pools ──────────────────────────────────────────────────────

    /// <summary>
    /// Sessions eligible as the first Vestibular onboarding experience.
    /// isOnboardingEligible = true, primaryZone = Vestibular.
    /// Used by the Vestibular gate system (Sprint 6).
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
    /// isRecoverySession = true, primaryZone = Vestibular.
    /// Used by UnlockEngine (Sprint 6).
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
