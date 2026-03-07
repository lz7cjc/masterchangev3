// ═══════════════════════════════════════════════════════════════════════════
// RewardConfig.cs  —  Assets/Scripts/RewardConfig.cs
//
// VERSION:  v1
// DATE:     2026-02-23
//
// NEW FILE — created as part of v2 schema migration.
// Reward calculation moved out of CSV and SessionData into this
// configurable ScriptableObject so it can be tuned between builds
// without touching session assets.
//
// CREATE ASSET:
//   Right-click in Project → Create → MasterChange → Reward Config
//   Place in Assets/Config/
//   Drag into SessionRegistry (or VRVideoController) Inspector slot.
//
// USAGE IN VRVideoController (or equivalent):
//   float watchFraction = (float)(videoPlayer.time / videoPlayer.length);
//   int reward = rewardConfig.Calculate(currentSession.level, watchFraction);
//   // Award reward to player...
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

[CreateAssetMenu(fileName = "RewardConfig", menuName = "MasterChange/Reward Config")]
public class RewardConfig : ScriptableObject
{
    [Header("Reward Formula")]
    [Tooltip("Divides level × watchFraction to produce the reward. Default 15 = modest reward even for short watches.")]
    [Range(1f, 100f)]
    public float divisor = 15f;

    [Tooltip("Multiply by this to scale rewards globally. 1.0 = no scaling.")]
    public float globalMultiplier = 1f;

    /// <summary>
    /// Calculate reward given a session level and fraction watched (0–1).
    /// Formula: ceil(level × watchFraction / divisor × globalMultiplier)
    /// Returns 0 if watchFraction is 0 or level is 0.
    /// </summary>
    public int Calculate(int level, float watchFraction)
    {
        if (watchFraction <= 0f || level <= 0) return 0;
        float raw = level * Mathf.Clamp01(watchFraction) / divisor * globalMultiplier;
        return Mathf.CeilToInt(raw);
    }
}
