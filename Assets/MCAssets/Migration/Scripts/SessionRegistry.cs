// SessionRegistry.cs
// Assets/MCAssets/Migration/Scripts/SessionRegistry.cs
//
// VERSION:   v4
// CREATED:   2026-03-07
// UPDATED:   2026-03-22
// TIMESTAMP: 2026-03-22T12:00:00Z
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
//   v4  2026-03-22  Fix stale Instance ghost between Play sessions.
//                   ROOT CAUSE: When entering Play a second time, Instance still
//                   held a reference to the destroyed component from the previous
//                   session. Unity's == operator returns true for destroyed objects
//                   compared to null, but the duplicate Awake check uses != null
//                   which evaluates as true against the ghost. This causes the
//                   NEW (valid) component to be treated as the duplicate and
//                   destroyed, clearing Instance via OnDestroy. One frame later
//                   ConstellationManager finds Instance == null.
//                   FIX: Instance is declared as a plain static field and explicitly
//                   reset via [RuntimeInitializeOnLoadMethod(SubsystemRegistration)]
//                   which fires before any Awake on every Play press, guaranteeing
//                   a clean slate regardless of previous session state.
//   v3  2026-03-22  Remove DontDestroyOnLoad. Destroy(this) for duplicates.
//   v2  2026-03-15  GetMindfulnessPool() added.
//   v1  2026-03-07  Initial creation.
//
// OBSOLETE FILES:
//   SessionRegistry.cs v3 (2026-03-22)
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

    // Plain static field — Unity's destroyed-object magic cannot interfere with
    // a raw field the way it can with properties backed by Unity Object references.
    public static SessionRegistry Instance;

    // Fires before ANY Awake() on every Play press — clears the stale reference
    // left over from the previous Play session before any component initialises.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        Debug.Log("[SessionRegistry] Static reset — clean slate for this Play session.");
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A live instance already exists — this is a genuine duplicate.
            Debug.LogWarning($"[SessionRegistry] Duplicate detected on '{gameObject.name}' " +
                             $"— destroying this component. Live instance is on " +
                             $"'{Instance.gameObject.name}'.");
            Destroy(this);
            return;
        }

        Instance = this;
        Debug.Log($"[SessionRegistry] Awake — singleton set on '{gameObject.name}'. " +
                  $"{allSessions.Count} session(s) pre-loaded.");
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
    /// Used by PostSessionController.CheckMindfulnessTrigger() to randomly select
    /// a Mindfulness session to offer the user when thresholds are met.
    /// Mindfulness sessions suppress the post-session form and standard Riros
    /// reward — only the presence bonus is awarded.
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
    /// isOnboardingEligible = true, primaryZone = Vestibular.
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
                Debug.Log($"[SessionRegistry]   {zone}: {count} session(s)");
        }
    }
}
