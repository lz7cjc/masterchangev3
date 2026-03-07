// ═══════════════════════════════════════════════════════════════════════════
// SessionData.cs
// Assets/MCAssets/Migration/Scripts/SessionData.cs
//
// VERSION:  v6                          DATE: 2026-03-07
// TIMESTAMP: 2026-03-07T12:00:00Z
//
// ████████████████████████████████████████████████████████████████████████
// THIS IS THE CANONICAL BASELINE FOR THIS PROJECT.
// ALL PREVIOUS VERSIONS ARE OBSOLETE.
// DO NOT USE ANY EARLIER VERSION OF THIS SCRIPT.
// ████████████████████████████████████████████████████████████████████████
//
// CHANGE LOG:
//   v6  2026-03-07  THREAD-CANONICAL RE-ISSUE
//     - Produced from Setup Guide v6.3 as the authoritative source
//     - unlockCondition confirmed as int (v2 spec). MockUserProgress had a
//       stale string-split pattern referencing this field — that is a bug in
//       MockUserProgress, not here. See note at bottom of this file.
//     - rirosReward confirmed ABSENT. Calculated at runtime by RirosManager.
//       MockUserProgress.CompleteAll() had a stale s.rirosReward reference —
//       fix: replace with MarkCompleted(s.sessionID) using default rirosEarned.
//     - All enums verified against Setup Guide v6.3 ENUMS section.
//       PhobiaZone: 13 values (None + 12 active zones), serialisation order locked.
//     - OBSOLETE: any SessionData file with a _vN suffix
//
//   v5  2026-03-06  ZONE LIST UPDATE
//     - Flying reinstated, OpenSpaces added, Insects added, FoodContamination added
//     - All appended — serialisation order preserved
//
//   v4  2026-03-06  ENUM DEFINITIONS RESTORED + motionProfile TYPE CORRECTED
//     - CS0246 root cause fix: enums were accidentally dropped from the file
//     - motionProfile type corrected string → MotionProfile enum
//     - isRecoverySession + isOnboardingEligible added (vestibular_zone_spec)
//
//   v3  2026-03-06  motionProfile string field added (SUPERSEDED — type was wrong)
//
//   v2  2026-02-23  V2 SCHEMA MIGRATION
//     - REMOVED: rirosReward, durationSeconds
//     - ADDED:   gcsFilename, urlVerified, urlLastChecked,
//                locationSlug, dateShot, editVersion
//     - unlockCondition: string → int
//     - level range: 1–10 → 0–10
//     - additionalZones: List<PhobiaZone> added
//
//   v1  Original — do not use
//
// ── OBSOLETE FILES — DELETE IF PRESENT ──────────────────────────────────────
//   SessionData_v5.cs, SessionData_v4.cs, SessionData_v3.cs,
//   SessionData_v2.cs, SessionData_v1.cs
//   (Only SessionData.cs — no version suffix — should exist in the project)
//
// ── WHERE ALL ENUMS LIVE ─────────────────────────────────────────────────────
//   ALL enum definitions are in THIS file. There is no separate enums file.
//     PhobiaZone, BehaviourZone, WorldType, AntechamberAsset, MotionProfile
//   Scripts that depend on these types:
//     ConstellationOrb.cs, CrossoverConnector.cs, MockUserProgress.cs,
//     SessionRegistry.cs, ConstellationManager.cs, SessionScanner.cs
//
// ── CRITICAL: ENUM ORDERING RULES ───────────────────────────────────────────
//   NEVER insert a value between existing enum entries.
//   ALWAYS append new values at the end (before the closing brace).
//   Inserting mid-enum shifts integer backing values and corrupts all
//   serialised SessionData assets in the Unity project.
//
// ── rirosReward MUST NOT EXIST ON THIS CLASS ─────────────────────────────────
//   Removed in v2. Calculated at runtime by RirosManager.
//   Formula: Level 0 = 5 Riros. Level n = 10 + (n-1)*5.
//   Do NOT add it back here. Do NOT recreate AutoFillRiros().
//
//   KNOWN ISSUE IN MockUserProgress.cs (pre-existing, not caused by this file):
//     CompleteAll() calls s.rirosReward — stale reference from before v2.
//     Fix: replace with MarkCompleted(s.sessionID) — uses default rirosEarned = 10.
//     Also: EvaluateUnlocks() parses session.unlockCondition as a string —
//     stale pre-v2 pattern. unlockCondition is an int. Replace with int comparison.
//
// FILE LOCATION:
//   Assets/MCAssets/Migration/Scripts/SessionData.cs
//   Save WITHOUT a version suffix — Unity uses the class name, not the filename.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MasterChange/Session Data", fileName = "NewSession")]
public class SessionData : ScriptableObject
{
    // ── Core identity ─────────────────────────────────────────────────────────
    [Header("Core Identity")]
    [Tooltip("Unique identifier. Format: ZONE_LN or ZONE_LN_seq. " +
             "Must match CSV SessionID and GCS filename stem.")]
    public string sessionID;

    // ── Zone classification ───────────────────────────────────────────────────
    [Header("Zone Classification")]
    [Tooltip("Primary zone — controls which constellation cluster the orb appears in.")]
    public PhobiaZone primaryZone;

    [Tooltip("Also therapeutically valid for these zones. Used by recommendation engine. " +
             "A session can relate to more than one phobia.")]
    public List<PhobiaZone> additionalZones = new List<PhobiaZone>();

    public BehaviourZone behaviourZone;
    public WorldType     worldType;

    // ── Display ───────────────────────────────────────────────────────────────
    [Header("Display")]
    public string displayTitle;

    [TextArea(2, 5)]
    public string description;

    // ── Video ─────────────────────────────────────────────────────────────────
    [Header("Video")]
    public string videoURL;
    public string gcsFilename;
    public bool   urlVerified;
    public string urlLastChecked;

    // ── Session properties ────────────────────────────────────────────────────
    [Header("Session Properties")]
    [Range(0, 10)]
    [Tooltip("Therapeutic level. 0 = pre-programme gate session. 1–10 = progressive exposure.")]
    public int level;

    [Tooltip("Integer unlock gate. 0 = always available. " +
             "n = requires level n to be completed in this zone first.")]
    public int unlockCondition;

    public bool       isCrossover;
    public PhobiaZone crossoverSourceZone;

    [Tooltip("If true: post-session anxiety form suppressed, Riros award suppressed. " +
             "All Mindfulness sessions must have this true.")]
    public bool isMindfulnessSession;

    public AntechamberAsset antechamberAsset;

    // ── Vestibular flags ──────────────────────────────────────────────────────
    // Required by vestibular_zone_spec and SessionRegistry.GetVestibularRecoveryPool()
    // / GetVestibularOnboardingPool(). Do not remove.
    [Header("Vestibular")]
    [Tooltip("True for sessions used as recovery content mid-session. " +
             "Separate from isOnboardingEligible.")]
    public bool isRecoverySession;

    [Tooltip("True for L1–L2 Vestibular sessions eligible to play during onboarding. " +
             "Short, stable, maximum horizon-lock.")]
    public bool isOnboardingEligible;

    // ── Production tracking ───────────────────────────────────────────────────
    [Header("Production Tracking")]
    public string locationSlug;
    public string dateShot;
    public string editVersion;

    [TextArea(1, 3)]
    public string notes;

    // ── Motion profile ────────────────────────────────────────────────────────
    // Typed enum — NOT a string. Default Unclassified.
    // Used by the Vestibular gate and motion sickness tolerance system.
    // Fill in session_registry.csv then re-import via SessionScanner.
    [Header("Motion Profile")]
    [Tooltip(
        "Describes camera movement. Used by Vestibular gate to sequence sessions safely.\n\n" +
        "Unclassified  — not yet assessed. Shown to all users until tagged.\n" +
        "Static        — camera does not move. Safest.\n" +
        "SlowDrift     — gentle pan or slow forward movement.\n" +
        "SteadyForward — walking pace, stable horizon.\n" +
        "WindingRoad   — lateral turns, direction changes.\n" +
        "MultiAxis     — combined pitch/yaw/roll (boat, cable car, lift).\n" +
        "Aerial        — flight, drone, elevated moving POV."
    )]
    public MotionProfile motionProfile = MotionProfile.Unclassified;

    // ─────────────────────────────────────────────────────────────────────────
    // REMOVED FIELDS — DO NOT ADD BACK
    //
    //   int rirosReward     — removed v2. Calculated at runtime by RirosManager.
    //                         Formula: Level 0 = 5 Riros. Level n = 10 + (n-1)*5.
    //   int durationSeconds — removed v2. Not needed at asset level.
    // ─────────────────────────────────────────────────────────────────────────
}


// ═══════════════════════════════════════════════════════════════════════════
// ENUMS
// All enums defined in this file — there is no separate enums file.
//
// Consumers: ConstellationOrb.cs, CrossoverConnector.cs, MockUserProgress.cs,
//            SessionRegistry.cs, ConstellationManager.cs, SessionScanner.cs
//
// CRITICAL: NEVER insert values between existing entries. ALWAYS append.
//           Inserting mid-enum shifts integer backing values and corrupts
//           all serialised SessionData assets in the project.
// ═══════════════════════════════════════════════════════════════════════════

public enum PhobiaZone
{
    None,               // 0 — default / unassigned
    Heights,            // 1
    Water,              // 2
    Sharks,             // 3
    Crowds,             // 4
    ClosedSpaces,       // 5 — was Claustrophobia, renamed T00
    Mindfulness,        // 6 — support zone: NOT spawned in constellation arc; triggered contextually
    Mountains,          // 7 — adventure zone
    Vestibular,         // 8 — VR comfort + motion sickness tolerance (see vestibular_zone_spec)
    OpenSpaces,         // 9 — Agoraphobia; appended T00
    Flying,             // 10 — reinstated; was archived T00, reconfirmed active
    Insects,            // 11 — Entomophobia; appended v5
    FoodContamination   // 12 — Mysophobia; appended v5
    // REMOVED: Anxiety — not a zone; all phobias are anxiety-related (V2-01)
    // CRITICAL: ALWAYS APPEND. NEVER INSERT MID-ENUM.
}

public enum BehaviourZone
{
    None,
    Smoking,
    Alcohol
    // Expandable — append only (sleep, focus, stress, etc.)
}

public enum WorldType
{
    Constellation,      // Phobia therapy — Constellation Sky world
    BehaviouralChange   // Behaviour change programmes — Cottage World
    // NOTE: legacy serialised assets may contain 'Phobia' (old name for Constellation).
    // Use Find/Replace in CSV before re-importing if upgrading old assets.
}

public enum AntechamberAsset
{
    None,
    Airport,    // Legacy — retained for serialisation safety; no active sessions currently use this
    Water,      // Water + Sharks zones
    Cottage     // BehaviouralChange zones (Smoking, Alcohol)
}

public enum MotionProfile
{
    Unclassified = 0,   // Default — shown to all users; shown first while library is being tagged
    Static,             // Camera does not move — safest
    SlowDrift,          // Gentle pan or slow forward movement
    SteadyForward,      // Walking pace, stable horizon
    WindingRoad,        // Lateral turns, vehicle or path changes direction
    MultiAxis,          // Combined pitch/yaw/roll — boats, cable cars, lifts
    Aerial              // Flight, drone, elevated moving POV
    // ALWAYS APPEND. NEVER INSERT MID-ENUM.
}
