// MockUserProgress.cs
// Assets/MCAssets/Migration/Scripts/MockUserProgress.cs
//
// VERSION:  5.0
// DATE:     2026-03-19
// TIMESTAMP: 2026-03-19T00:00:00Z
//
// CHANGE LOG:
//   v5.0  2026-03-19  PROJECTCONFIG INTEGRATION — BAKU THRESHOLDS DE-HARDCODED
//     - Hardcoded Baku stage thresholds (>= 3 for Stage 2, >= 6 for Stage 3)
//       removed from EvaluateUnlocks().
//     - Thresholds now read from ProjectConfig.bakuStage2Threshold and
//       ProjectConfig.bakuStage3Threshold.
//     - [SerializeField] private ProjectConfig _projectConfig slot added.
//     - If _projectConfig is null, falls back to hardcoded values with a
//       LogWarning so unset Inspector slots surface immediately.
//     - OBSOLETE: MockUserProgress.cs v4.0
//
//   v4.0  2026-03-09  ZonePrefix() switch removed — ZoneConfig.GetPrefix() lookup.
//   v3.0  2026-03-07  All 12 zones. Vestibular gate. motionSicknessLevel.
//   v2.0             unlockCondition string→int. rirosReward removed.
//   v1.0             Initial implementation.
//
// OBSOLETE FILES: None — same canonical filename, version tracked in header.
//
// PURPOSE:
//   Local stand-in for the Supabase backend during pre-S4 development.
//   Replaced by UserProgressService.cs in Sprint 4 — public API is identical.
//
// INSPECTOR SETUP — NEW IN v5.0:
//   Drag ProjectConfig.asset into the Project Config slot on GameManager.
//   Create via Assets → Create → MasterChange → Project Config if not present.
//
// DEPENDENCIES:
//   ZoneConfig.asset    — drag into Inspector. Prefix lookups.
//   ProjectConfig.asset — drag into Inspector. Baku stage thresholds.
//
// FILE LOCATION:
//   Assets/MCAssets/Migration/Scripts/MockUserProgress.cs
// ═══════════════════════════════════════════════════════════════════════════════

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

    // ── Project Config ────────────────────────────────────────────────────────
    [Header("Project Config")]
    [Tooltip("Drag ProjectConfig.asset here. Provides Baku stage thresholds. " +
             "Create via Assets → Create → MasterChange → Project Config if not present.")]
    [SerializeField] private ProjectConfig _projectConfig;

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
    private Dictionary<string, OrbState> _sessionStates    = new Dictionary<string, OrbState>();
    private HashSet<string>              _completedSessions = new HashSet<string>();
    private Dictionary<PhobiaZone, int>  _repeatCounts      = new Dictionary<PhobiaZone, int>();
    private int                          _rirosBalance      = 0;
    private int                          _bakuStage         = 1;

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

        // Baku stage — thresholds read from ProjectConfig.
        // Falls back to hardcoded defaults with a warning if ProjectConfig not assigned.
        int stage2 = _projectConfig != null ? _projectConfig.bakuStage2Threshold : FallbackStage2();
        int stage3 = _projectConfig != null ? _projectConfig.bakuStage3Threshold : FallbackStage3();

        _bakuStage = _completedSessions.Count >= stage3 ? 3
                   : _completedSessions.Count >= stage2 ? 2
                   : 1;
    }

    private int FallbackStage2()
    {
        Debug.LogWarning("[MockProgress] ProjectConfig not assigned — using fallback bakuStage2Threshold = 3. " +
                         "Drag ProjectConfig.asset into the Project Config slot on MockUserProgress.");
        return 3;
    }

    private int FallbackStage3()
    {
        Debug.LogWarning("[MockProgress] ProjectConfig not assigned — using fallback bakuStage3Threshold = 6. " +
                         "Drag ProjectConfig.asset into the Project Config slot on MockUserProgress.");
        return 6;
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
