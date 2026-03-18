// ═══════════════════════════════════════════════════════════════════════════
// SessionScanner.cs
// Assets/Editor/SessionScanner.cs
//
// VERSION:  v10
// DATE:     2026-03-15
// TIMESTAMP: 2026-03-15T00:00:00Z
//
// ████████████████████████████████████████████████████████████████████████
// THIS IS THE BASELINE. ALL PREVIOUS VERSIONS ARE OBSOLETE.
// DO NOT USE ANY EARLIER VERSION OF THIS SCRIPT.
// ████████████████████████████████████████████████████████████████████████
//
// CHANGE LOG:
//   v10  2026-03-15  CANONICAL MERGE — two diverged v9 files resolved into one
//     - FILE MUST LIVE IN Assets/Editor/ — confirmed authoritative location.
//       Any copy outside Assets/Editor/ must be deleted.
//     - Hardcoded ZONE_MAP dictionary removed (was in the 2026-03-14 Editor
//       version). Zone resolution now fully delegated to ZoneConfig.GetZoneFromString()
//       in ParseGCSPath() — consistent with the no-hardcoding rule.
//     - ZONE_MAP and BEHAVIOUR_ZONES static fields removed entirely.
//     - ParseGCSPath() now takes ZoneConfig parameter (from v9.0 ZoneConfig version).
//     - ScanGCSFileList() passes ZoneConfig to ParseGCSPath() (from v9.0).
//     - Comment-line skipping (#) added to ImportFromCsv() (from v9.0).
//     - ParseCsvLine() replaces SplitCsv() — same logic, clearer name.
//     - ValidateAll() replaces ValidateAllSessions() — proper errors/warnings
//       lists, better dialog messaging (from v9.0).
//     - AutoFillUnlockConditions() added (from v9.0).
//     - AutoFillAntechambers() added (from v9.0).
//     - OBSOLETE: any SessionScanner at Assets/MCAssets/Migration/ (wrong location)
//     - OBSOLETE: any SessionScanner at Assets/MCAssets/Migration/FilmDatabase/
//
//   v9   2026-03-14  CRITICAL LOCATION FIX (Editor-only version)
//     - Moved to Assets/Editor/ — UnityEditor APIs unavailable at runtime.
//     - Every "type 'MenuItem' could not be found" error was caused by wrong location.
//
//   v9.0 2026-03-09  ZONE_MAP dictionary removed (ZoneConfig version)
//     - ParseGCSPath() delegates to ZoneConfig.GetZoneFromString().
//     - Adding a new zone requires only: PhobiaZone enum append + ZoneConfig row.
//     - No code change to this script when adding zones.
//
//   v8   2026-03-06  ZONE LIST UPDATE — Flying reinstated, Insects + FoodContamination added
//   v7   2026-03-06  BUG FIX — motionProfile type corrected, AutoFillRiros removed
//   v6   2026-02-27  MotionProfile column added (broken — fixed in v7)
//   v5   2026-02-26  Baseline synchronisation
//   v3   2026-02-23  v2 CSV schema (21 columns)
//
// OBSOLETE FILES — DELETE ALL OF THESE:
//   Assets/MCAssets/Migration/Scripts/SessionScanner.cs      ← DELETE
//   Assets/MCAssets/Migration/FilmDatabase/SessionScanner.cs ← DELETE
//   Any SessionScanner with a number suffix anywhere in project ← DELETE
//
// FILE LOCATION (canonical):
//   E:\Development\MC_V3_clean\Assets\Editor\SessionScanner.cs
//
// DEPENDENCY:
//   ZoneConfig.asset — must exist at Assets/MCAssets/Migration/ZoneConfig.asset
//   SessionData.cs   — defines PhobiaZone, BehaviourZone, WorldType, AntechamberAsset, MotionProfile
//
// AUTHORITATIVE CSV SCHEMA — 22 columns (matches name_films_routes.py):
//   SessionID, PrimaryZone, AdditionalZones, BehaviourZone, WorldType,
//   DisplayTitle, Description, VideoURL,
//   Level, UnlockCondition,
//   IsCrossover, CrossoverSourceZone, IsMindfulness,
//   AntechamberAsset, GCSFilename, URLVerified, URLLastChecked,
//   LocationSlug, DateShot, EditVersion, Notes,
//   MotionProfile
//
// WORKFLOW SEQUENCE:
//   1. name_films_routes.py  — name sessions, write session_registry.csv
//   2. gcs_uploader.py       — verify/upload to GCS, set URLVerified
//   3. supabase_sync.py      — push verified rows to Supabase
//   4. SessionScanner.cs     ← YOU ARE HERE
//      MasterChange → Import Sessions from CSV → creates SessionData assets
//      MasterChange → Validate All Sessions    → confirm all fields correct
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
    private const string GCS_LIST_PATH    = "Assets/MCAssets/Migration/FilmDatabase/gcs_file_list.txt";
    private const string CSV_OUTPUT_PATH  = "Assets/MCAssets/Migration/FilmDatabase/sessions_scan_output.csv";
    private const string CSV_IMPORT_PATH  = "Assets/MCAssets/Migration/FilmDatabase/session_registry.csv";
    private const string ZONE_CONFIG_PATH = "Assets/MCAssets/Migration/ZoneConfig.asset";

    private const string GCS_BASE_URL = "https://storage.googleapis.com/masterchange/Phobias";

    // ── Zone config loader ───────────────────────────────────────────────────
    private static ZoneConfig LoadZoneConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<ZoneConfig>(ZONE_CONFIG_PATH);
        if (cfg == null)
            Debug.LogError($"[Scanner] ZoneConfig not found at {ZONE_CONFIG_PATH}. " +
                           "Create it via Assets → Create → MasterChange → Zone Config " +
                           "and populate all active zones.");
        return cfg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Scan existing session assets → CSV (audit / export)
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Scan Existing Session Assets → CSV")]
    public static void ScanExistingAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var rows = new List<string[]> { CsvHeader() };

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
                sd.motionProfile.ToString(),
            });
        }

        WriteCsv(rows, CSV_OUTPUT_PATH);
        Debug.Log($"[Scanner] Exported {rows.Count - 1} existing session assets to {CSV_OUTPUT_PATH}");
        EditorUtility.RevealInFinder(CSV_OUTPUT_PATH);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Scan GCS file list → generate import-ready CSV
    // Zone resolution uses ZoneConfig — no hardcoded zone list.
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Scan GCS File List → Sessions CSV")]
    public static void ScanGCSFileList()
    {
        ZoneConfig zoneCfg = LoadZoneConfig();
        if (zoneCfg == null) return;

        if (!File.Exists(GCS_LIST_PATH))
        {
            EditorUtility.DisplayDialog("File Not Found",
                $"Expected file not found:\n{GCS_LIST_PATH}\n\n" +
                "Generate it by running:\n\n" +
                "gsutil ls -r gs://masterchange/Phobias/** > " +
                "Assets/MCAssets/Migration/FilmDatabase/gcs_file_list.txt\n\n" +
                "Then re-run this menu item.",
                "OK");
            return;
        }

        string[] lines = File.ReadAllLines(GCS_LIST_PATH);
        var rows = new List<string[]> { CsvHeader() };

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

            ParsedSession ps = ParseGCSPath(line, zoneCfg);
            if (ps == null)
            {
                Debug.LogWarning($"[Scanner] Could not parse (zone not found in ZoneConfig): {line}");
                skipped++;
                continue;
            }

            string dupNote = existingAssets.Contains(ps.SessionID) ? "EXISTS" : "";

            rows.Add(new string[] {
                ps.SessionID,
                ps.PrimaryZone,
                "",
                ps.BehaviourZone,
                ps.WorldType,
                "",
                "",
                ps.VideoURL,
                ps.Level,
                ps.UnlockCondition.ToString(),
                "",
                "",
                ps.IsMindfulness ? "true" : "",
                ps.Antechamber,
                ps.GCSFilename,
                "false",
                "",
                "",
                "",
                "v1",
                dupNote != "" ? $"DUPLICATE: already imported as {ps.SessionID}" : "",
                "",
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
    // Uses System.Enum.TryParse — no zone list needed here.
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

        // Skip comment lines (lines beginning with # — version-control metadata)
        var dataLines = lines.Where(l => !l.TrimStart().StartsWith("#")).ToArray();
        if (dataLines.Length < 2)
        {
            EditorUtility.DisplayDialog("Empty CSV", "The CSV file contains no data rows.", "OK");
            return;
        }

        string[] headers = ParseCsvLine(dataLines[0]);
        var colIndex = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            colIndex[headers[i].Trim()] = i;

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

        string assetFolder = "Assets/MCAssets/Migration/Sessions";
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            AssetDatabase.CreateFolder("Assets/MCAssets/Migration", "Sessions");
            AssetDatabase.Refresh();
        }

        int created = 0, updated = 0, skipped = 0;

        for (int i = 1; i < dataLines.Length; i++)
        {
            string line = dataLines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCsvLine(line);
            string sessionID = GetCol(cols, colIndex, "SessionID");
            if (string.IsNullOrEmpty(sessionID)) { skipped++; continue; }

            string zoneStr = GetCol(cols, colIndex, "PrimaryZone");
            if (!System.Enum.TryParse(zoneStr, out PhobiaZone primaryZone))
            {
                Debug.LogWarning($"[Scanner] Unknown PrimaryZone '{zoneStr}' for session '{sessionID}' — skipping.");
                skipped++;
                continue;
            }

            string zoneFolder = $"{assetFolder}/{primaryZone}";
            if (!AssetDatabase.IsValidFolder(zoneFolder))
            {
                AssetDatabase.CreateFolder(assetFolder, primaryZone.ToString());
                AssetDatabase.Refresh();
            }

            string assetPath = $"{zoneFolder}/{sessionID}.asset";
            SessionData sd = AssetDatabase.LoadAssetAtPath<SessionData>(assetPath);
            bool isNew = sd == null;
            if (isNew)
                sd = ScriptableObject.CreateInstance<SessionData>();
            else
                Undo.RecordObject(sd, "Import session from CSV");

            sd.sessionID      = sessionID;
            sd.primaryZone    = primaryZone;
            sd.displayTitle   = GetCol(cols, colIndex, "DisplayTitle");
            sd.description    = GetCol(cols, colIndex, "Description");
            sd.videoURL       = GetCol(cols, colIndex, "VideoURL");
            sd.gcsFilename    = GetCol(cols, colIndex, "GCSFilename");
            sd.locationSlug   = GetCol(cols, colIndex, "LocationSlug");
            sd.dateShot       = GetCol(cols, colIndex, "DateShot");
            sd.editVersion    = GetCol(cols, colIndex, "EditVersion");
            sd.notes          = GetCol(cols, colIndex, "Notes");
            sd.urlLastChecked = GetCol(cols, colIndex, "URLLastChecked");

            sd.urlVerified = GetCol(cols, colIndex, "URLVerified")
                .Equals("true", System.StringComparison.OrdinalIgnoreCase);

            if (int.TryParse(GetCol(cols, colIndex, "Level"), out int level))
                sd.level = level;

            if (int.TryParse(GetCol(cols, colIndex, "UnlockCondition"), out int unlockCond))
                sd.unlockCondition = unlockCond;

            sd.isCrossover = GetCol(cols, colIndex, "IsCrossover")
                .Equals("true", System.StringComparison.OrdinalIgnoreCase);

            sd.isMindfulnessSession = GetCol(cols, colIndex, "IsMindfulness")
                .Equals("true", System.StringComparison.OrdinalIgnoreCase);

            if (System.Enum.TryParse(GetCol(cols, colIndex, "BehaviourZone"), out BehaviourZone bz))
                sd.behaviourZone = bz;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "WorldType"), out WorldType wt))
                sd.worldType = wt;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "CrossoverSourceZone"), out PhobiaZone cz))
                sd.crossoverSourceZone = cz;

            if (System.Enum.TryParse(GetCol(cols, colIndex, "AntechamberAsset"), out AntechamberAsset aa))
                sd.antechamberAsset = aa;

            string addlZonesStr = GetCol(cols, colIndex, "AdditionalZones");
            if (!string.IsNullOrEmpty(addlZonesStr))
            {
                sd.additionalZones = addlZonesStr.Split(';')
                    .Select(z => z.Trim())
                    .Where(z => System.Enum.TryParse(z, out PhobiaZone _))
                    .Select(z => (PhobiaZone)System.Enum.Parse(typeof(PhobiaZone), z))
                    .ToList();
            }

            sd.motionProfile = System.Enum.TryParse(GetCol(cols, colIndex, "MotionProfile"), out MotionProfile mp)
                ? mp : MotionProfile.Unclassified;

            if (isNew) { AssetDatabase.CreateAsset(sd, assetPath); created++; }
            else       { EditorUtility.SetDirty(sd);               updated++; }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Scanner] Import complete. Created: {created}, Updated: {updated}, Skipped: {skipped}.");
        EditorUtility.DisplayDialog("Import Complete",
            $"Created:  {created} new session assets\n" +
            $"Updated:  {updated} existing assets\n" +
            $"Skipped:  {skipped} empty/invalid rows\n\n" +
            "Run MasterChange → Validate All Sessions to check for errors.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Validate all session assets
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Validate All Sessions")]
    public static void ValidateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var errors   = new List<string>();
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
                errors.Add($"{sd.sessionID}: level {sd.level} out of range (0–10)");
            if (string.IsNullOrEmpty(sd.displayTitle))
                warnings.Add($"{sd.sessionID}: displayTitle is empty");
            if (sd.motionProfile == MotionProfile.Unclassified)
                warnings.Add($"{sd.sessionID}: motionProfile is Unclassified");
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
            foreach (var e in errors)   Debug.LogError($"[Scanner] ERROR: {e}");
            foreach (var w in warnings) Debug.LogWarning($"[Scanner] WARN: {w}");

            EditorUtility.DisplayDialog("Validation Complete",
                $"Validated {total} sessions.\n\n" +
                $"Errors:   {errors.Count} (fix before shipping)\n" +
                $"Warnings: {warnings.Count} (review before shipping)\n\n" +
                "MotionProfile warnings are expected until all sessions are tagged.",
                "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Auto-fill unlock conditions
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("MasterChange/Auto-Fill Unlock Conditions")]
    public static void AutoFillUnlockConditions()
    {
        string[] guids = AssetDatabase.FindAssets("t:SessionData");
        var allSessions = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<SessionData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(sd => sd != null).ToList();

        int updated = 0;

        var byZone = allSessions
            .Where(sd => !sd.isMindfulnessSession)
            .GroupBy(sd => sd.primaryZone);

        foreach (var zoneGroup in byZone)
        {
            foreach (var levelGroup in zoneGroup.GroupBy(sd => sd.level).OrderBy(g => g.Key))
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
        Debug.Log($"[Scanner] Auto-filled unlock conditions on {updated} session assets.");
        EditorUtility.DisplayDialog("Done",
            $"Updated {updated} unlock conditions.\n\nRun MasterChange → Validate All Sessions to confirm.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Auto-fill antechambers
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
                PhobiaZone.Water  => AntechamberAsset.Water,
                PhobiaZone.Sharks => AntechamberAsset.Water,
                _                 => AntechamberAsset.None
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
        EditorUtility.DisplayDialog("Done",
            $"Set antechambers on {updated} sessions.\n\nRun MasterChange → Validate All Sessions to confirm.", "OK");
    }

    // NOTE: AutoFillRiros() permanently removed in v7.
    // rirosReward removed from SessionData v2 schema. Riros calculated at runtime by RirosManager.cs.

    // ── Helpers ───────────────────────────────────────────────────────────────

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
    }

    /// <summary>
    /// Infers session fields from a GCS path.
    /// Zone resolution is fully delegated to ZoneConfig — no hardcoded zone list.
    /// Adding a new zone requires only: PhobiaZone enum append + ZoneConfig row.
    /// </summary>
    private static ParsedSession ParseGCSPath(string gcsPath, ZoneConfig zoneCfg)
    {
        string path = Regex.Replace(gcsPath, @"^gs://[^/]+/", "").Replace("\\", "/");
        string[] parts = path.Split('/');
        if (parts.Length < 3) return null;

        ZoneEntry matchedEntry = null;
        string matchedFolder   = null;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            PhobiaZone resolved = zoneCfg.GetZoneFromString(parts[i]);
            if (resolved != PhobiaZone.None)
            {
                matchedEntry  = zoneCfg.GetEntry(resolved);
                matchedFolder = parts[i];
                break;
            }
        }

        if (matchedEntry == null) return null;

        string zoneName  = matchedEntry.zone.ToString();
        bool   isMindful = matchedEntry.zone == PhobiaZone.Mindfulness;
        string fileName  = Path.GetFileNameWithoutExtension(parts.Last());

        string levelStr = "1";
        Match levelMatch = Regex.Match(fileName, @"_[Ll](\d+)");
        if (levelMatch.Success)
            levelStr = levelMatch.Groups[1].Value;
        else
        {
            Match numMatch = Regex.Match(fileName, @"_(\d+)$");
            if (numMatch.Success) levelStr = numMatch.Groups[1].Value;
        }

        int level           = int.TryParse(levelStr, out int lv) ? lv : 1;
        int unlockCondition = Mathf.Max(0, level - 1);

        string ante = matchedEntry.zone switch
        {
            PhobiaZone.Water  => "Water",
            PhobiaZone.Sharks => "Water",
            _                 => "None"
        };

        // Build video URL from matched folder position
        int folderIndex = System.Array.IndexOf(parts, matchedFolder);
        string urlSuffix = folderIndex >= 0
            ? string.Join("/", parts.Skip(folderIndex))
            : parts.Last();

        return new ParsedSession
        {
            SessionID       = fileName,
            PrimaryZone     = zoneName,
            BehaviourZone   = "None",
            WorldType       = "Constellation",
            VideoURL        = $"{GCS_BASE_URL}/{urlSuffix}",
            GCSFilename     = parts.Last(),
            Level           = levelStr,
            UnlockCondition = unlockCondition,
            Antechamber     = ante,
            IsMindfulness   = isMindful,
        };
    }

    private static string[] CsvHeader() => new[]
    {
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
        var result    = new List<string>();
        bool inQuotes = false;
        var current   = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static void WriteCsv(List<string[]> rows, string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(cell =>
            {
                if (cell == null) return "";
                if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n"))
                    return "\"" + cell.Replace("\"", "\"\"") + "\"";
                return cell;
            })));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }
}
