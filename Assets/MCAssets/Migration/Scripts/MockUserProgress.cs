// ═══════════════════════════════════════════════════════════════════════════
// MockUserProgress.cs
// Assets/MCAssets/Migration/Scripts/MockUserProgress.cs
//
// VERSION:  v2                          DATE: 2026-03-07
// TIMESTAMP: 2026-03-07T12:00:00Z
//
// CHANGE LOG:
//   v2  2026-03-07  BUG FIXES — stale pre-v2 field references removed
//     - EvaluateUnlocks(): removed stale string.IsNullOrEmpty(unlockCondition)
//       and string.Split() pattern. unlockCondition is an int (since SessionData v2).
//       Replaced with: unlock if unlockCondition > 0 AND the previous session
//       in the same zone (by level order) is completed. This mirrors the intent
//       of the original string logic and matches what the real UnlockEngine will do.
//     - CompleteAll(): removed s.rirosReward reference (field does not exist —
//       removed from SessionData in v2). Replaced with MarkCompleted(s.sessionID)
//       which uses the default rirosEarned = 10.
//     - No other logic changes. All public API preserved.
//     - OBSOLETE: MockUserProgress.cs (v1, 2026-03-07)
//
//   v1  2026-03-07  Initial creation
//
// PURPOSE:
//   Local stand-in for the Supabase backend during pre-backend development.
//   Stores session state in memory. When the real backend is ready, replace
//   calls to MockUserProgress with calls to UserProgressService — the
//   constellation code does not need to change.
//
//   Attach to the same persistent GameObject as SessionRegistry.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MockUserProgress — local stand-in for the Supabase backend during pre-backend development.
/// Stores session state in memory.
/// When the real backend is ready, replace calls to MockUserProgress with
/// calls to UserProgressService — the constellation code doesn't need to change.
///
/// Attach to the same persistent GameObject as SessionRegistry.
/// </summary>
public class MockUserProgress : MonoBehaviour
{
    public static MockUserProgress Instance { get; private set; }

    // ── Orb visual states ─────────────────────────────────────────────────────
    public enum OrbState { Locked, Available, Recommended, Completed }

    // ── Internal state ────────────────────────────────────────────────────────
    private Dictionary<string, OrbState> _sessionStates    = new Dictionary<string, OrbState>();
    private HashSet<string>              _completedSessions = new HashSet<string>();
    private int _rirosBalance = 0;
    private int _bakuStage    = 1;

    [Header("Preset for testing — set states in Inspector")]
    public List<SessionStateOverride> testOverrides = new List<SessionStateOverride>();

    [System.Serializable]
    public class SessionStateOverride
    {
        public string   sessionID;
        public OrbState state;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyTestOverrides();
    }

    // ── Public API — mirrors what the real backend will return ────────────────

    /// <summary>Get the visual state of any orb.</summary>
    public OrbState GetOrbState(string sessionID)
    {
        if (_sessionStates.TryGetValue(sessionID, out var state))
            return state;
        return OrbState.Locked;
    }

    /// <summary>Mark a session as completed. Call after a test session ends.</summary>
    public void MarkCompleted(string sessionID, int rirosEarned = 10)
    {
        _completedSessions.Add(sessionID);
        _sessionStates[sessionID] = OrbState.Completed;
        _rirosBalance += rirosEarned;
        Debug.Log($"[MockProgress] {sessionID} completed. Riros balance: {_rirosBalance}");
        EvaluateUnlocks();
    }

    public int  GetRirosBalance()            => _rirosBalance;
    public int  GetBakuStage()               => _bakuStage;
    public bool IsCompleted(string sessionID) => _completedSessions.Contains(sessionID);

    // ── Unlock evaluation ─────────────────────────────────────────────────────
    // unlockCondition is an int (SessionData v2+):
    //   0  = always available
    //   n  = requires level n to be completed in the same zone first
    //
    // Logic: for each session not yet in _sessionStates, check whether
    // any lower-level session in the same zone with level == unlockCondition
    // has been completed. If so, mark this session Available.
    //
    // This mirrors what the real UnlockEngine will do in Sprint 5.

    private void EvaluateUnlocks()
    {
        if (SessionRegistry.Instance == null) return;

        foreach (var session in SessionRegistry.Instance.allSessions)
        {
            // Skip sessions that already have an explicit state set
            if (_sessionStates.ContainsKey(session.sessionID)) continue;

            if (session.unlockCondition == 0)
            {
                // Always available — make sure it is marked so
                _sessionStates[session.sessionID] = OrbState.Available;
                continue;
            }

            // Find the gate session: same zone, level == unlockCondition
            var gateSessions = SessionRegistry.Instance.GetByPhobiaZone(session.primaryZone);
            bool gateCleared = false;

            foreach (var gate in gateSessions)
            {
                if (gate.level == session.unlockCondition && _completedSessions.Contains(gate.sessionID))
                {
                    gateCleared = true;
                    break;
                }
            }

            if (gateCleared)
            {
                _sessionStates[session.sessionID] = OrbState.Available;
                Debug.Log($"[MockProgress] Unlocked: {session.sessionID} (gate level {session.unlockCondition} cleared)");
            }
        }

        // Update Baku companion stage
        int totalCompleted = _completedSessions.Count;
        _bakuStage = totalCompleted >= 6 ? 3 : totalCompleted >= 3 ? 2 : 1;
    }

    private void ApplyTestOverrides()
    {
        foreach (var o in testOverrides)
        {
            _sessionStates[o.sessionID] = o.state;
            if (o.state == OrbState.Completed)
                _completedSessions.Add(o.sessionID);
        }
    }

    // ── Editor helpers ────────────────────────────────────────────────────────

    [ContextMenu("Reset All Progress")]
    public void ResetAll()
    {
        _sessionStates.Clear();
        _completedSessions.Clear();
        _rirosBalance = 0;
        _bakuStage    = 1;
        ApplyTestOverrides();
        Debug.Log("[MockProgress] All progress reset.");
    }

    [ContextMenu("Complete All Sessions (Test)")]
    public void CompleteAll()
    {
        if (SessionRegistry.Instance == null) return;
        foreach (var s in SessionRegistry.Instance.allSessions)
            MarkCompleted(s.sessionID); // rirosEarned defaults to 10 — rirosReward does not exist on SessionData
    }
}
