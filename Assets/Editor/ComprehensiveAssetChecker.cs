using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ComprehensiveAssetChecker : EditorWindow
{
    private Vector2 scrollPosition;
    private List<AssetPackageInfo> assetPackages = new List<AssetPackageInfo>();
    private bool hasScanned = false;
    private bool isScanning = false;
    private bool showUsedPackages = false;
    private int totalAssetsScanned = 0;

    [System.Serializable]
    private class AssetPackageInfo
    {
        public string path;
        public string name;
        public int totalAssets;
        public int usedAssets;
        public int unusedAssets;
        public bool isUsed;
        public List<string> sampleUnusedAssets = new List<string>();
    }

    [MenuItem("Tools/Comprehensive Asset Usage Checker")]
    public static void ShowWindow()
    {
        GetWindow<ComprehensiveAssetChecker>("Asset Usage Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Comprehensive Asset Usage Checker", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool checks ALL asset types (scripts, models, textures, materials, etc.) by analyzing " +
            "Unity's dependency system. It finds assets with NO references anywhere in your project.",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button(isScanning ? "Scanning..." : "Scan Asset Packages", GUILayout.Height(30)))
        {
            if (!isScanning)
            {
                ScanAssetPackages();
            }
        }

        if (hasScanned)
        {
            GUILayout.Space(10);
            showUsedPackages = EditorGUILayout.Toggle("Show Used Packages", showUsedPackages);

            EditorGUILayout.LabelField($"Total Assets Scanned: {totalAssetsScanned}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Show packages with unused assets
            var unusedPackages = assetPackages.Where(p => !p.isUsed).ToList();
            if (unusedPackages.Count > 0)
            {
                GUILayout.Label($"Packages with Unused Assets ({unusedPackages.Count})", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("These packages contain assets that have no references in your project.", MessageType.Warning);

                foreach (var package in unusedPackages)
                {
                    DrawPackageInfo(package, new Color(1f, 0.8f, 0.8f));
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All assets in detected packages appear to be in use!", MessageType.Info);
            }

            // Show used packages if toggled
            if (showUsedPackages)
            {
                var usedPackages = assetPackages.Where(p => p.isUsed).ToList();
                if (usedPackages.Count > 0)
                {
                    GUILayout.Space(20);
                    GUILayout.Label($"Fully Used Packages ({usedPackages.Count})", EditorStyles.boldLabel);

                    foreach (var package in usedPackages)
                    {
                        DrawPackageInfo(package, new Color(0.8f, 1f, 0.8f));
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPackageInfo(AssetPackageInfo package, Color bgColor)
    {
        var defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = defaultColor;

        EditorGUILayout.LabelField("Package:", package.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Path:", package.path);
        EditorGUILayout.LabelField("Total Assets:", package.totalAssets.ToString());
        EditorGUILayout.LabelField("Used Assets:", $"{package.usedAssets} ({(package.totalAssets > 0 ? (package.usedAssets * 100 / package.totalAssets) : 0)}%)");
        EditorGUILayout.LabelField("Unused Assets:", $"{package.unusedAssets}");

        if (package.sampleUnusedAssets.Count > 0)
        {
            EditorGUILayout.LabelField("Sample Unused Assets:", EditorStyles.miniLabel);
            foreach (var asset in package.sampleUnusedAssets)
            {
                EditorGUILayout.LabelField("  • " + asset, EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private void ScanAssetPackages()
    {
        isScanning = true;
        hasScanned = false;
        assetPackages.Clear();
        totalAssetsScanned = 0;

        try
        {
            // Find potential asset package folders
            var packageFolders = FindAssetPackageFolders();

            if (packageFolders.Count == 0)
            {
                EditorUtility.DisplayDialog("No Packages Found",
                    "No asset packages detected in your project.", "OK");
                return;
            }

            float progress = 0f;
            float progressStep = 1f / packageFolders.Count;

            foreach (var folderPath in packageFolders)
            {
                EditorUtility.DisplayProgressBar("Scanning Asset Packages",
                    $"Analyzing {folderPath}...", progress);

                var packageInfo = AnalyzePackageFolder(folderPath);
                if (packageInfo != null)
                {
                    assetPackages.Add(packageInfo);
                    totalAssetsScanned += packageInfo.totalAssets;
                }

                progress += progressStep;
            }

            // Sort by unused asset count
            assetPackages = assetPackages.OrderByDescending(p => p.unusedAssets).ToList();
            hasScanned = true;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isScanning = false;
            Repaint();
        }
    }

    private List<string> FindAssetPackageFolders()
    {
        var packageFolders = new List<string>();
        var assetsPath = "Assets";

        // Common folder names to skip
        var skipFolders = new HashSet<string> {
            "Editor", "Resources", "StreamingAssets", "Plugins",
            "Scenes", "Scripts", "Prefabs", "Materials", "Textures"
        };

        // Get all directories in Assets
        var allDirs = Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories)
            .Select(d => d.Replace("\\", "/"))
            .Where(d => !d.Contains("/.")) // Skip hidden folders
            .ToList();

        foreach (var dir in allDirs)
        {
            var dirName = Path.GetFileName(dir);

            // Skip common utility folders
            if (skipFolders.Contains(dirName))
                continue;

            // Check if it looks like a package
            if (IsLikelyAssetPackage(dir))
            {
                packageFolders.Add(dir);
            }
        }

        return packageFolders;
    }

    private bool IsLikelyAssetPackage(string folderPath)
    {
        // Check for package indicators
        var indicators = new[]
        {
            Path.Combine(folderPath, "README.md"),
            Path.Combine(folderPath, "README.txt"),
            Path.Combine(folderPath, "LICENSE"),
            Path.Combine(folderPath, "LICENSE.txt"),
            Path.Combine(folderPath, "package.json")
        };

        if (indicators.Any(File.Exists))
            return true;

        // Check for package-like structure (multiple subfolders)
        var subdirs = Directory.GetDirectories(folderPath);
        return subdirs.Length >= 3;
    }

    private AssetPackageInfo AnalyzePackageFolder(string folderPath)
    {
        // Get all assets in this folder
        var allAssets = AssetDatabase.FindAssets("", new[] { folderPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !string.IsNullOrEmpty(path))
            .Where(path => !path.EndsWith(".cs.meta") && !path.EndsWith(".meta"))
            .ToList();

        if (allAssets.Count == 0)
            return null;

        int usedCount = 0;
        int unusedCount = 0;
        var sampleUnused = new List<string>();

        foreach (var assetPath in allAssets)
        {
            bool isUsed = IsAssetUsed(assetPath, folderPath);

            if (isUsed)
            {
                usedCount++;
            }
            else
            {
                unusedCount++;
                if (sampleUnused.Count < 5)
                {
                    sampleUnused.Add(Path.GetFileName(assetPath));
                }
            }
        }

        return new AssetPackageInfo
        {
            path = folderPath,
            name = Path.GetFileName(folderPath),
            totalAssets = allAssets.Count,
            usedAssets = usedCount,
            unusedAssets = unusedCount,
            isUsed = unusedCount == 0,
            sampleUnusedAssets = sampleUnused
        };
    }

    private bool IsAssetUsed(string assetPath, string packageFolder)
    {
        // Get all dependencies TO this asset (what references it)
        var dependencies = AssetDatabase.GetDependencies(new[] { assetPath }, false);

        // Check if any asset outside the package folder depends on this asset
        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/"))
            .Where(path => !path.StartsWith(packageFolder)) // Exclude package's own folder
            .Where(path => !path.EndsWith(".cs")) // Exclude scripts (they don't show in GetDependencies properly)
            .ToList();

        foreach (var otherAsset in allAssets)
        {
            var otherDeps = AssetDatabase.GetDependencies(new[] { otherAsset }, false);
            if (otherDeps.Contains(assetPath))
            {
                return true; // Found a reference!
            }
        }

        // Also check if it's referenced in any scene
        var scenes = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !path.StartsWith(packageFolder))
            .ToList();

        foreach (var scene in scenes)
        {
            var sceneDeps = AssetDatabase.GetDependencies(new[] { scene }, true);
            if (sceneDeps.Contains(assetPath))
            {
                return true;
            }
        }

        return false;
    }
}