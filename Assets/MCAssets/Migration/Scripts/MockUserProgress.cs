// MockUserProgress.cs
// Assets/MCAssets/Migration/Scripts/MockUserProgress.cs
//
// VERSION:  4.0
// TIMESTAMP: 2026-03-09T00:00:00Z
//
// CHANGE LOG:
//   v4.0  2026-03-09  ZonePrefix() switch statement removed — replaced with
//                     ZoneConfig.GetPrefix() lookup. No zone metadata hardcoded
//                     in this script. ZoneConfig asset reference added.
//   v3.0  2026-03-07  All 12 zones added to ZonePrefix() switch. Vestibular gate,
//                     motionSicknessLevel, GetRepeatCount, HasAnyCompletion added.
//   v2.0             unlockCondition changed string→int. rirosReward removed.
//   v1.0             Initial implementation.
//
// OBSOLETE FILES: None — same filename, version tracked in header.
//
// PURPOSE:
//   Local stand-in for the Supabase backend during pre-S4 development.
//   Replaced by UserProgressService.cs in Sprint 4 — public API is identical.
//
// DEPENDENCY: ZoneConfig.asset — drag into Inspector field on GameManager.

using System.Collections.Generic;
using UnityEngine;

public class MockUserProgress : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static MockUserProgress Instance { get; private set; }

    // ── Orb visual states — DO NOT REORDER (ConstellationOrb serialises these) ─
    public enum OrbState { Locked, Available, Recommended, Completed }

    // ── Zone Config ───────────────────────────────────────────────────────────
    [Header("Zone Config")]
    [Tooltip("Drag ZoneConfig.asset here. Used for prefix lookups — no zone data hardcoded.")]
    public ZoneConfig zoneConfig;

    // ── Vestibular gate ───────────────────────────────────────────────────────
    [Header("Vestibular / VR Onboarding")]
    [Tooltip("Set true in Inspector to simulate VR onboarding complete. " +
             "All non-Vestibular zones unlock when this is true.")]
    public bool vrOnboardingComplete = false;

    [Tooltip("0 = none, 1 = mild, 2 = moderate, 3 = severe")]
    [Range(0, 3)]
    public int motionSicknessLevel = 0;

    [Tooltip("Flagged by HeadsetMonitor or self-report. Pauses progression.")]
    public bool motionSicknessFlagged = false;

    // ── Test overrides ────────────────────────────────────────────────────────
    [Header("Test Overrides — set session states in Inspector")]
    public List<SessionStateOverride> testOverrides = new List<SessionStateOverride>();

    [System.Serializable]
    public class SessionStateOverride
    {
        public string   sessionID;
        public OrbState state;
    }

    // ── Internal state ────────────────────────────────────────────────────────
    private Dictionary<string, OrbState> _sessionStates     = new Dictionary<string, OrbState>();
    private HashSet<string>              _completedSessions  = new HashSet<string>();
    private Dictionary<PhobiaZone, int>  _repeatCounts       = new Dictionary<PhobiaZone, int>();
    private int                          _rirosBalance       = 0;
    private int                          _bakuStage          = 1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyTestOverrides();
    }

    // ── Public API — identical signatures to UserProgressService ──────────────

    public OrbState GetOrbState(string sessionID)
    {
        if (_sessionStates.TryGetValue(sessionID, out var state)) return state;
        return OrbState.Locked;
    }

    public void MarkCompleted(string sessionID)
    {
        if (_completedSessions.Contains(sessionID)) return;

        _completedSessions.Add(sessionID);
        _sessionStates[sessionID] = OrbState.Completed;

        SessionData session = SessionRegistry.Instance?.GetSession(sessionID);
        if (session != null)
        {
            PhobiaZone zone = session.primaryZone;
            _repeatCounts[zone] = _repeatCounts.ContainsKey(zone) ? _repeatCounts[zone] + 1 : 1;
        }

        Debug.Log($"[MockProgress] Completed: {sessionID}");
        EvaluateUnlocks();
    }

    public bool IsCompleted(string sessionID)
        => _completedSessions.Contains(sessionID);

    public bool IsZoneAccessible(PhobiaZone zone)
        => zone == PhobiaZone.Vestibular || vrOnboardingComplete;

    public int GetRepeatCount(PhobiaZone zone)
        => _repeatCounts.ContainsKey(zone) ? _repeatCounts[zone] : 0;

    public bool HasAnyCompletion(PhobiaZone zone)
    {
        if (SessionRegistry.Instance == null) return false;
        foreach (var s in SessionRegistry.Instance.GetByPhobiaZone(zone))
            if (_completedSessions.Contains(s.sessionID)) return true;
        return false;
    }

    public int GetRirosBalance() => _rirosBalance;
    public int GetBakuStage()    => _bakuStage;

    /// <summary>
    /// 3-letter zone prefix. Reads from ZoneConfig asset — no hardcoded list.
    /// Falls back to "UNK" if ZoneConfig is not assigned or zone not found.
    /// </summary>
    public string ZonePrefix(PhobiaZone zone)
    {
        if (zoneConfig == null)
        {
            Debug.LogWarning("[MockProgress] ZoneConfig not assigned — cannot resolve prefix.");
            return "UNK";
        }
        return zoneConfig.GetPrefix(zone);
    }

    // ── Unlock evaluation ─────────────────────────────────────────────────────
    private void EvaluateUnlocks()
    {
        if (SessionRegistry.Instance == null) return;

        foreach (var session in SessionRegistry.Instance.allSessions)
        {
            if (_sessionStates.ContainsKey(session.sessionID)) continue;
            if (session.unlockCondition == 0)
            {
                _sessionStates[session.sessionID] = OrbState.Available;
                continue;
            }

            int zoneCompletions = GetRepeatCount(session.primaryZone);
            if (zoneCompletions >= session.unlockCondition)
            {
                _sessionStates[session.sessionID] = OrbState.Available;
                Debug.Log($"[MockProgress] Unlocked: {session.sessionID}");
            }
        }

        _bakuStage = _completedSessions.Count >= 6 ? 3
                   : _completedSessions.Count >= 3 ? 2
                   : 1;
    }

    // ── Test overrides ────────────────────────────────────────────────────────
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
        _repeatCounts.Clear();
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
            MarkCompleted(s.sessionID);
    }

    [ContextMenu("Set VR Onboarding Complete (Test)")]
    public void SetVROnboardingComplete()
    {
        vrOnboardingComplete = true;
        Debug.Log("[MockProgress] vrOnboardingComplete = true. All zones now accessible.");
    }
}
