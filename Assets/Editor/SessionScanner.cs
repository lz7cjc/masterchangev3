// ═══════════════════════════════════════════════════════════════════════════
// SessionScanner.cs
// Assets/MCAssets/Migration/FilmDatabase/SessionScanner.cs
//
// VERSION:  v8                          DATE: 2026-03-06
// TIMESTAMP: 2026-03-06T12:00:00Z
//
// ████████████████████████████████████████████████████████████████████████
// THIS IS THE BASELINE. ALL PREVIOUS VERSIONS ARE OBSOLETE.
// DO NOT USE ANY EARLIER VERSION OF THIS SCRIPT.
// ████████████████████████████████████████████████████████████████████████
//
// CHANGE LOG:
//   v8  2026-03-06  ZONE LIST UPDATE
//     - ZONE_MAP: Flying reinstated (was omitted T00 — reconfirmed active)
//     - ZONE_MAP: Insects added (Entomophobia)
//     - ZONE_MAP: FoodContamination added (Mysophobia)
//     - Stale header comment updated (no longer says Flying removed)
//     - Antechamber note updated (Flying antechamber = None for now)
//     - DEPENDENCY: SessionData.cs (PhobiaZone enum updated)
//     - OBSOLETE: SessionScanner.cs
//
//   v7  2026-03-06  BUG FIX — TWO COMPILER ERRORS FROM v6 RESOLVED
//                   + motionProfile type corrected string → MotionProfile enum
//
//     ERROR 1 — CS1061: 'SessionData' does not contain 'motionProfile'
//       Root cause: motionProfile was added to the CSV schema (v6) but was
//       never added to SessionData.cs.
//       Fix: resolved by SessionData.cs which adds the MotionProfile enum
//             field. ScanExistingAssets() calls .ToString() for CSV export.
//             ImportFromCsv() parses string → MotionProfile enum.
//             Unclassified is the safe default for any unrecognised string.
//
//     ERROR 2 — CS1061: 'SessionData' does not contain 'rirosReward'
//       Root cause: rirosReward was REMOVED from SessionData in the v2
//       schema migration (Sprint 1). SessionScanner_v6 re-introduced a
//       reference to it in AutoFillRiros() — a regression.
//       Fix: AutoFillRiros() method removed entirely.
//             Riros rewards are calculated at runtime by RirosManager.cs.
//
//     ERROR 3 (new in v3 → v4) — CS0246: PhobiaZone / BehaviourZone /
//             WorldType / AntechamberAsset could not be found
//       Root cause: SessionData_v3 extracted the class into a new file but
//             did NOT carry the enum definitions. All four enums were left
//             undefined. SessionData_v4 restores them at bottom of file.
//       This scanner does not define enums — it depends on SessionData_v4.
//
//   ── DEPENDENCY: SessionData.cs ──────────────────────────────────────
//     Requires SessionData.cs in the same assembly. That file defines:
//       SessionData class, PhobiaZone, BehaviourZone, WorldType,
//       AntechamberAsset, MotionProfile enums.
//
//   v6  2026-02-27  MOTIONPROFILE COLUMN ADDED (broken — see above)
//   v5  2026-02-26  BASELINE SYNCHRONISATION
//   v3  2026-02-23  v2 CSV SCHEMA (21 columns)
//
// OBSOLETE FILES — DELETE THESE:
//   SessionScanner.cs  (superseded)
//   SessionData_v3.cs     (superseded — wrong motionProfile type, missing enums)
//   SessionScanner_v6.cs  (superseded — two CS1061 errors, see above)
//   SessionScanner_v5.cs  (superseded)
//   SessionScanner_v3.cs  (superseded)
//   SessionScanner_v2.cs  (superseded)
//
// AUTHORITATIVE CSV SCHEMA — 22 columns (matches name_films_routes.py):
//   SessionID, PrimaryZone, AdditionalZones, BehaviourZone, WorldType,
//   DisplayTitle, Description, VideoURL,
//   Level, UnlockCondition,
//   IsCrossover, CrossoverSourceZone, IsMindfulness,
//   AntechamberAsset, GCSFilename, URLVerified, URLLastChecked,
//   LocationSlug, DateShot, EditVersion, Notes,
//   MotionProfile  (v6 — Unclassified|Static|SlowDrift|SteadyForward|WindingRoad|MultiAxis|Aerial)
//
// FILE LOCATIONS (v7 canonical):
//   This script:   E:\Development\MC_V3_clean\Assets\MCAssets\Migration\
//                  FilmDatabase\SessionScanner.cs
//   CSV (import):  E:\Development\MC_V3_clean\Assets\MCAssets\Migration\
//                  FilmDatabase\session_registry.csv
//                  (auto-copied here from NamingFilms\Outputs\ on each Save)
//
// WORKFLOW SEQUENCE:
//   1. name_films_routes.py  — name sessions, write session_registry.csv
//   2. gcs_uploader.py     — verify/upload to GCS, set URLVerified
//   3. supabase_sync.py    — push verified rows to Supabase
//   4. SessionScanner.cs   ← YOU ARE HERE
//      MasterChange → Import Sessions from CSV → creates SessionData assets
//      MasterChange → Validate All Sessions    → confirm all fields correct
//
// GCS SCAN WORKFLOW (alternative — when starting from raw GCS file list):
//   1. gsutil ls -r gs://masterchange/** > gcs_file_list.txt
//   2. Copy to Assets/MCAssets/Migration/FilmDatabase/gcs_file_list.txt
//   3. MasterChange → Scan GCS File List → Sessions CSV
//   4. Open generated CSV, fill DisplayTitle / Description / Notes / MotionProfile
//   5. MasterChange → Import Sessions from CSV
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

public class SessionScanner
{
    // ── PATHS ────────────────────────────────────────────────────────────────
    // FilmDatabase is the canonical Unity folder for all session registry files.
    private const string GCS_LIST_PATH   = "Assets/MCAssets/Migration/FilmDatabase/gcs_file_list.txt";
    private const string LOCAL_ROOT      = "Assets/MCAssets/Migration/FilmDatabase";
    private const string CSV_OUTPUT_PATH = "Assets/MCAssets/Migration/FilmDatabase/sessions_scan_output.csv";
    // CSV written by name_films_routes.py and auto-copied here on each Save:
    private const string CSV_IMPORT_PATH = "Assets/MCAssets/Migration/FilmDatabase/session_registry.csv";

    // ── GCS BUCKET BASE URL ─────────────────────────────────────────────────
    // Change this to match your actual bucket URL prefix.
    private const string GCS_BASE_URL = "https://storage.googleapis.com/YOUR-BUCKET/sessions";

    // ── ZONE FOLDER MAP ─────────────────────────────────────────────────────
    // v7 — corrected to match confirmed zone list:
    //   Added:   ClosedSpaces (renamed from Claustrophobia), OpenSpaces, Vestibular
    //   Reinstated: Flying (reconfirmed active v8)
    //   Added (v8): Insects, FoodContamination
    // Legacy keys retained for GCS path backwards compatibility during T00 migration.
    private static readonly Dictionary<string, string> ZONE_MAP = new(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Flying",            "Flying"           },
        { "Heights",           "Heights"          },
        { "Water",             "Water"            },
        { "Sharks",            "Sharks"           },
        { "Crowds",            "Crowds"           },
        { "ClosedSpaces",      "ClosedSpaces"     },
        { "OpenSpaces",        "OpenSpaces"       },
        { "Mindfulness",       "Mindfulness"      },
        { "Mountains",         "Mountains"        },
        { "Vestibular",        "Vestibular"       },
        { "Insects",           "Insects"          },
        { "FoodContamination", "FoodContamination"},
        { "Smoking",           "Smoking"          },
        { "Alcohol",           "Alcohol"          },
        // ── Legacy keys — GCS folders not yet renamed (T00 migration) ───────
        { "Claustrophobia",    "ClosedSpaces"     },   // maps old folder → new zone name
        { "MFN",               "Mindfulness"      },   // legacy folder name
        // Anxiety intentionally omitted — not a zone; all phobias are anxiety-related.
    };

    private static readonly Dictionary<string, string> BEHAVIOUR_ZONES = new()
    {
        { "Smoking", "Smoking" },
        { "Alcohol", "Alcohol" },
    };

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Scan existing session assets → CSV (audit / export)
    // Exports all existing SessionData ScriptableObjects to CSV for review.
    // Requires SessionData_v3 (motionProfile field). If not yet updated,
    // this method will throw CS1061 — apply SessionData.cs first.
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Scan Existing Session Assets → CSV")]
    public static void ScanExistingAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var rows = new List<string[]>();
        rows.Add(CsvHeader());

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SessionData sd = AssetDatabase.LoadAssetAtPath<SessionData>(path);
            if (sd == null) continue;

            rows.Add(new string[] {
                sd.sessionID,
                sd.primaryZone.ToString(),
                string.Join(";", sd.additionalZones?.Select(z => z.ToString()) ?? new string[0]),
                sd.behaviourZone.ToString(),
                sd.worldType.ToString(),
                sd.displayTitle,
                sd.description,
                sd.videoURL,
                sd.level.ToString(),
                sd.unlockCondition.ToString(),
                sd.isCrossover ? "true" : "",
                sd.crossoverSourceZone.ToString(),
                sd.isMindfulnessSession ? "true" : "",
                sd.antechamberAsset.ToString(),
                sd.gcsFilename,
                sd.urlVerified ? "true" : "false",
                sd.urlLastChecked,
                sd.locationSlug,
                sd.dateShot,
                sd.editVersion,
                sd.notes,
                sd.motionProfile.ToString(),   // v7 — MotionProfile enum → string for CSV
            });
        }

        WriteCsv(rows, CSV_OUTPUT_PATH);
        Debug.Log($"[Scanner] Exported {rows.Count - 1} existing session assets to {CSV_OUTPUT_PATH}");
        EditorUtility.RevealInFinder(CSV_OUTPUT_PATH);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Scan GCS file list → generate import-ready CSV
    // Reads gcs_file_list.txt and infers SessionData fields from paths/names.
    // Use when importing sessions that haven't been through the namer yet.
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Scan GCS File List → Sessions CSV")]
    public static void ScanGCSFileList()
    {
        if (!File.Exists(GCS_LIST_PATH))
        {
            EditorUtility.DisplayDialog("File Not Found",
                $"Expected file not found:\n{GCS_LIST_PATH}\n\n" +
                "Generate it by running this command in your terminal:\n\n" +
                "gsutil ls -r gs://YOUR-BUCKET/sessions/** > Assets/MCAssets/Migration/FilmDatabase/gcs_file_list.txt\n\n" +
                "Then re-run this menu item.",
                "OK");
            return;
        }

        string[] lines = File.ReadAllLines(GCS_LIST_PATH);
        var rows = new List<string[]>();
        rows.Add(CsvHeader());

        var existingAssets = AssetDatabase.FindAssets("t:SessionData")
            .Select(g => AssetDatabase.LoadAssetAtPath<SessionData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(sd => sd != null)
            .Select(sd => sd.sessionID)
            .ToHashSet();

        int parsed = 0, skipped = 0, duplicates = 0;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string lower = line.ToLower();
            if (!lower.EndsWith(".mp4") && !lower.EndsWith(".mov") && !lower.EndsWith(".webm"))
            {
                skipped++;
                continue;
            }

            ParsedSession ps = ParseGCSPath(line);
            if (ps == null)
            {
                Debug.LogWarning($"[Scanner] Could not parse (check zone folder name — Anxiety removed, Claustrophobia renamed to ClosedSpaces): {line}");
                skipped++;
                continue;
            }

            string dupNote = existingAssets.Contains(ps.SessionID) ? "EXISTS" : "";

            rows.Add(new string[] {
                ps.SessionID,
                ps.PrimaryZone,
                "",                  // AdditionalZones — fill manually if needed
                ps.BehaviourZone,
                ps.WorldType,
                "",                  // DisplayTitle — fill in spreadsheet
                "",                  // Description — fill in spreadsheet
                ps.VideoURL,
                ps.Level,
                ps.UnlockCondition.ToString(),
                "",                  // IsCrossover
                "",                  // CrossoverSourceZone
                ps.IsMindfulness ? "true" : "",
                ps.Antechamber,
                ps.GCSFilename,
                "false",             // URLVerified — not yet checked
                "",                  // URLLastChecked
                "",                  // LocationSlug — fill in spreadsheet
                "",                  // DateShot — fill in spreadsheet
                "v1",                // EditVersion — default
                dupNote != "" ? $"DUPLICATE: already imported as {ps.SessionID}" : "",
                "",                  // MotionProfile — fill in spreadsheet (v6+)
            });

            if (dupNote == "EXISTS") duplicates++;
            parsed++;
        }

        WriteCsv(rows, CSV_OUTPUT_PATH);
        Debug.Log($"[Scanner] Parsed {parsed} files. Skipped {skipped}. {duplicates} already exist. CSV: {CSV_OUTPUT_PATH}");
        EditorUtility.RevealInFinder(CSV_OUTPUT_PATH);

        EditorUtility.DisplayDialog("Scan Complete",
            $"Found: {parsed} video files\n" +
            $"Already imported: {duplicates}\n" +
            $"Skipped (not video / unrecognised zone): {skipped}\n\n" +
            $"CSV saved to:\n{CSV_OUTPUT_PATH}\n\n" +
            "Open it in Google Sheets or Excel.\n" +
            "Fill in: DisplayTitle, Description, LocationSlug, DateShot, MotionProfile.\n" +
            "Delete the 'EXISTS' rows if you don't want to re-import.\n\n" +
            "Then run: MasterChange → Import Sessions from CSV",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Import sessions from CSV → SessionData ScriptableObjects
    // Reads session_registry.csv and creates/updates SessionData assets.
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Import Sessions from CSV")]
    public static void ImportFromCsv()
    {
        if (!File.Exists(CSV_IMPORT_PATH))
        {
            EditorUtility.DisplayDialog("File Not Found",
                $"CSV not found at:\n{CSV_IMPORT_PATH}\n\n" +
                "Run name_films_routes.py and ensure session_registry.csv " +
                "has been copied to the FilmDatabase folder.",
                "OK");
            return;
        }

        string[] lines = File.ReadAllLines(CSV_IMPORT_PATH, Encoding.UTF8);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Empty CSV", "The CSV file contains no data rows.", "OK");
            return;
        }

        // Parse header to build column index map
        string[] headers = ParseCsvLine(lines[0]);
        var colIndex = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            colIndex[headers[i].Trim()] = i;

        // Required columns check
        string[] required = { "SessionID", "PrimaryZone", "WorldType", "VideoURL", "Level" };
        var missing = required.Where(c => !colIndex.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("CSV Schema Error",
                $"Missing required columns:\n{string.Join(", ", missing)}\n\n" +
                "Ensure this CSV was generated by name_films_routes.py.",
                "OK");
            return;
        }

        string assetFolder = "Assets/MCAssets/Migration/FilmDatabase/Sessions";
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            AssetDatabase.CreateFolder("Assets/MCAssets/Migration/FilmDatabase", "Sessions");
            AssetDatabase.Refresh();
        }

        int created = 0, updated = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCsvLine(line);
            string sessionID = GetCol(cols, colIndex, "SessionID");
            if (string.IsNullOrEmpty(sessionID)) { skipped++; continue; }

            // Find existing asset or create new
            string assetPath = $"{assetFolder}/{sessionID}.asset";
            SessionData sd = AssetDatabase.LoadAssetAtPath<SessionData>(assetPath);
            bool isNew = sd == null;
            if (isNew)
            {
                sd = ScriptableObject.CreateInstance<SessionData>();
            }
            else
            {
                Undo.RecordObject(sd, "Import session from CSV");
            }

            // ── Populate fields ─────────────────────────────────────────────
            sd.sessionID     = sessionID;
            sd.displayTitle  = GetCol(cols, colIndex, "DisplayTitle");
            sd.description   = GetCol(cols, colIndex, "Description");
            sd.videoURL      = GetCol(cols, colIndex, "VideoURL");
            sd.gcsFilename   = GetCol(cols, colIndex, "GCSFilename");
            sd.locationSlug  = GetCol(cols, colIndex, "LocationSlug");
            sd.dateShot      = GetCol(cols, colIndex, "DateShot");
            sd.editVersion   = GetCol(cols, colIndex, "EditVersion");
            sd.notes         = GetCol(cols, colIndex, "Notes");
            sd.urlLastChecked = GetCol(cols, colIndex, "URLLastChecked");

            string urlVerifiedStr = GetCol(cols, colIndex, "URLVerified");
            sd.urlVerified = urlVerifiedStr.Equals("true", System.StringComparison.OrdinalIgnoreCase);

            if (int.TryParse(GetCol(cols, colIndex, "Level"), out int level))
                sd.level = level;

            if (int.TryParse(GetCol(cols, colIndex, "UnlockCondition"), out int unlockCond))
                sd.unlockCondition = unlockCond;

            string isCrossoverStr = GetCol(cols, colIndex, "IsCrossover");
            sd.isCrossover = isCrossoverStr.Equals("true", System.StringComparison.OrdinalIgnoreCase);

            string isMindfulStr = GetCol(cols, colIndex, "IsMindfulness");
            sd.isMindfulnessSession = isMindfulStr.Equals("true", System.StringComparison.OrdinalIgnoreCase);

            // Enums
            if (System.Enum.TryParse(GetCol(cols, colIndex, "PrimaryZone"), out PhobiaZone pz))
                sd.primaryZone = pz;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "BehaviourZone"), out BehaviourZone bz))
                sd.behaviourZone = bz;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "WorldType"), out WorldType wt))
                sd.worldType = wt;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "CrossoverSourceZone"), out PhobiaZone cz))
                sd.crossoverSourceZone = cz;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "AntechamberAsset"), out AntechamberAsset aa))
                sd.antechamberAsset = aa;

            // AdditionalZones — semicolon-separated
            string addlZonesStr = GetCol(cols, colIndex, "AdditionalZones");
            if (!string.IsNullOrEmpty(addlZonesStr))
            {
                sd.additionalZones = addlZonesStr.Split(';')
                    .Select(z => z.Trim())
                    .Where(z => System.Enum.TryParse(z, out PhobiaZone _))
                    .Select(z => (PhobiaZone)System.Enum.Parse(typeof(PhobiaZone), z))
                    .ToList();
            }

            // motionProfile — parse typed enum from CSV string (requires SessionData_v4)
            string motionProfileStr = GetCol(cols, colIndex, "MotionProfile");
            sd.motionProfile = System.Enum.TryParse(motionProfileStr, out MotionProfile mp)
                ? mp
                : MotionProfile.Unclassified;

            // ── Save ────────────────────────────────────────────────────────
            if (isNew)
            {
                AssetDatabase.CreateAsset(sd, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(sd);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Scanner] Import complete. Created: {created}, Updated: {updated}, Skipped: {skipped}.");
        EditorUtility.DisplayDialog("Import Complete",
            $"Created:  {created} new session assets\n" +
            $"Updated:  {updated} existing assets\n" +
            $"Skipped:  {skipped} empty/invalid rows\n\n" +
            "Run MasterChange → Validate All Sessions to check for errors.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Validate all session assets
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Validate All Sessions")]
    public static void ValidateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SessionData sd = AssetDatabase.LoadAssetAtPath<SessionData>(path);
            if (sd == null) continue;

            if (string.IsNullOrEmpty(sd.sessionID))
                errors.Add($"{path}: sessionID is empty");

            if (string.IsNullOrEmpty(sd.videoURL))
                warnings.Add($"{sd.sessionID}: videoURL is empty");

            if (!sd.urlVerified)
                warnings.Add($"{sd.sessionID}: URLVerified = false");

            if (sd.level < 0 || sd.level > 10)
                errors.Add($"{sd.sessionID}: level {sd.level} is out of range (0–10)");

            if (string.IsNullOrEmpty(sd.displayTitle))
                warnings.Add($"{sd.sessionID}: displayTitle is empty");

            if (sd.motionProfile == MotionProfile.Unclassified)
                warnings.Add($"{sd.sessionID}: motionProfile is Unclassified (tag manually in CSV then re-import)");
        }

        int total = guids.Length;

        if (errors.Count == 0 && warnings.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Passed",
                $"All {total} session assets validated — no errors or warnings.", "OK");
            Debug.Log($"[Scanner] Validation passed. {total} sessions OK.");
        }
        else
        {
            foreach (var e in errors)  Debug.LogError($"[Scanner] ERROR: {e}");
            foreach (var w in warnings) Debug.LogWarning($"[Scanner] WARN: {w}");

            EditorUtility.DisplayDialog("Validation Complete",
                $"Validated {total} sessions.\n\n" +
                $"Errors:   {errors.Count} (see Console — fix before shipping)\n" +
                $"Warnings: {warnings.Count} (see Console — review before shipping)\n\n" +
                "motionProfile warnings are expected until you fill those values in.",
                "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Auto-fill unlock conditions
    // For each zone, sorts sessions by level and writes the sequential
    // unlock condition as an INTEGER: L2 requires level 1, L3 requires level 2.
    // Level 1 (and level 0) sessions get unlock condition 0 (no prerequisite).
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Auto-Fill Unlock Conditions")]
    public static void AutoFillUnlockConditions()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var allSessions = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<SessionData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(sd => sd != null)
            .ToList();

        int updated = 0;

        var byZone = allSessions
            .Where(sd => !sd.isMindfulnessSession)
            .GroupBy(sd => sd.primaryZone);

        foreach (var zoneGroup in byZone)
        {
            var byLevel = zoneGroup.GroupBy(sd => sd.level).OrderBy(g => g.Key);

            foreach (var levelGroup in byLevel)
            {
                int unlockCondition = Mathf.Max(0, levelGroup.Key - 1);

                foreach (var sd in levelGroup.OrderBy(sd => sd.sessionID))
                {
                    if (sd.unlockCondition != unlockCondition)
                    {
                        Undo.RecordObject(sd, "Auto-fill unlock condition");
                        sd.unlockCondition = unlockCondition;
                        EditorUtility.SetDirty(sd);
                        updated++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Scanner] Auto-filled unlock conditions (integers) on {updated} session assets.");
        EditorUtility.DisplayDialog("Done",
            $"Updated {updated} unlock conditions.\n\nRun MasterChange → Validate All Sessions to confirm.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Auto-set antechambers by zone
    // Water → Water, Sharks → Water,
    // Smoking → Cottage, Alcohol → Cottage,
    // All phobia zones and Mindfulness → None
    //
    // v8 NOTE: Flying antechamber = None (Airport antechamber reserved for future use).
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Auto-Fill Antechambers by Zone")]
    public static void AutoFillAntechambers()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        int updated = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SessionData sd = AssetDatabase.LoadAssetAtPath<SessionData>(path);
            if (sd == null) continue;

            AntechamberAsset ante = sd.primaryZone switch
            {
                PhobiaZone.Water   => AntechamberAsset.Water,
                PhobiaZone.Sharks  => AntechamberAsset.Water,
                _ => AntechamberAsset.None
            };

            if (sd.worldType == WorldType.BehaviouralChange)
                ante = AntechamberAsset.Cottage;

            if (sd.antechamberAsset != ante)
            {
                Undo.RecordObject(sd, "Set antechamber");
                sd.antechamberAsset = ante;
                EditorUtility.SetDirty(sd);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Scanner] Set antechambers on {updated} sessions.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NOTE: AutoFillRiros() HAS BEEN REMOVED (v7)
    //
    // rirosReward was removed from SessionData in the v2 schema migration.
    // Riros rewards must NOT be stored on SessionData ScriptableObjects.
    // They are calculated at runtime by RirosManager.cs using this formula:
    //   Level 0  → 5 Riros
    //   Level n  → 10 + ((n - 1) * 5) Riros
    //
    // DO NOT add rirosReward back to SessionData.
    // DO NOT recreate AutoFillRiros().
    // ─────────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private class ParsedSession
    {
        public string SessionID;
        public string PrimaryZone;
        public string BehaviourZone;
        public string WorldType;
        public string VideoURL;
        public string GCSFilename;
        public string Level;
        public int    UnlockCondition;
        public string Antechamber;
        public bool   IsMindfulness;
        public string MotionProfile;   // blank by default — fill in spreadsheet
    }

    // Infers session fields from a GCS path like:
    //   gs://bucket/sessions/Phobia/Heights/APP_HGT_cliffs-dover_L3_122757-01-005_v1.mp4
    private static ParsedSession ParseGCSPath(string gcsPath)
    {
        string path = Regex.Replace(gcsPath, @"^gs://[^/]+/", "").Replace("\\", "/");

        string[] parts = path.Split('/');
        if (parts.Length < 3) return null;

        string zoneFolder = null;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (ZONE_MAP.ContainsKey(parts[i]))
            {
                zoneFolder = parts[i];
                break;
            }
        }

        if (zoneFolder == null) return null;

        string zoneName    = ZONE_MAP[zoneFolder];
        bool   isBehaviour = BEHAVIOUR_ZONES.ContainsKey(zoneName);
        bool   isMindful   = zoneName == "Mindfulness";

        string fileName = Path.GetFileNameWithoutExtension(parts.Last());

        // Infer level from file name: look for _L followed by digits
        string levelStr = "?";
        Match levelMatch = Regex.Match(fileName, @"_[Ll](\d+)");
        if (levelMatch.Success)
            levelStr = levelMatch.Groups[1].Value;
        else
        {
            Match numMatch = Regex.Match(fileName, @"_(\d+)$");
            if (numMatch.Success) levelStr = numMatch.Groups[1].Value;
        }

        int level = int.TryParse(levelStr, out int lv) ? lv : 1;
        int unlockCondition = Mathf.Max(0, level - 1);

        string videoURL = GCS_BASE_URL + "/" + string.Join("/", parts.Skip(
            parts.TakeWhile(p => !ZONE_MAP.ContainsKey(p)).Count() - 1));

        string ante = zoneName switch
        {
            "Water"  => "Water",
            "Sharks" => "Water",
            _ when isBehaviour => "Cottage",
            _ => "None"
        };

        return new ParsedSession
        {
            SessionID        = fileName,
            PrimaryZone      = isBehaviour ? "None"             : zoneName,
            BehaviourZone    = isBehaviour ? zoneName            : "None",
            WorldType        = isBehaviour ? "BehaviouralChange" : "Constellation",
            VideoURL         = videoURL,
            GCSFilename      = parts.Last(),
            Level            = levelStr,
            UnlockCondition  = unlockCondition,
            Antechamber      = ante,
            IsMindfulness    = isMindful,
            MotionProfile    = "",
        };
    }

    // ── CSV HEADER — must exactly match name_films_routes.py CSV_HEADERS ────
    // 22 columns. Any change here must be mirrored across all v6 scripts.
    private static string[] CsvHeader() => new string[] {
        "SessionID", "PrimaryZone", "AdditionalZones", "BehaviourZone", "WorldType",
        "DisplayTitle", "Description", "VideoURL",
        "Level", "UnlockCondition",
        "IsCrossover", "CrossoverSourceZone", "IsMindfulness",
        "AntechamberAsset", "GCSFilename", "URLVerified", "URLLastChecked",
        "LocationSlug", "DateShot", "EditVersion", "Notes",
        "MotionProfile"
    };

    private static string GetCol(string[] cols, Dictionary<string, int> idx, string name)
    {
        if (!idx.TryGetValue(name, out int i)) return "";
        if (i >= cols.Length) return "";
        return cols[i].Trim();
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static void WriteCsv(List<string[]> rows, string path)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(cell =>
            {
                if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n"))
                    return "\"" + cell.Replace("\"", "\"\"") + "\"";
                return cell;
            })));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }
}
