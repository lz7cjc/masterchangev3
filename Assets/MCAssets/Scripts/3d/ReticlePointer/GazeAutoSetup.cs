using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// FIXED VERSION: Handles MeshColliders and missing tags gracefully
/// Automatic setup tool for GazeReticlePointer system
/// </summary>
public class GazeAutoSetup : MonoBehaviour
{
    [Header("Setup Options")]
    [Tooltip("Apply to objects with these components")]
    public bool setupEnhancedVideoPlayers = true;
    public bool setupUIButtons = true;
    public bool setupSpriteRenderers = true;
    public bool setupTeleportSigns = true;

    [Header("Default Settings")]
    [Tooltip("Default hover delay for HUD elements")]
    public float hudHoverDelay = 2f;
    [Tooltip("Default hover delay for world objects")]
    public float worldObjectHoverDelay = 3f;

    [Header("HUD Detection")]
    [Tooltip("Objects with these names/tags are considered HUD elements")]
    public string[] hudIdentifiers = new string[] { "HUD", "Menu", "Button", "Panel" };

    [Header("Teleport Detection")]
    [Tooltip("Objects with these names are teleport signs")]
    public string[] teleportIdentifiers = new string[] { "sign", "Sign", "teleport", "Teleport", "location", "Location" };

    [Header("Preview")]
    [Tooltip("Show what will be modified without actually changing anything")]
    public bool previewMode = false;

    // Statistics
    private int enhancedVideoPlayersFound = 0;
    private int uiButtonsFound = 0;
    private int spriteRenderersFound = 0;
    private int teleportSignsFound = 0;
    private int triggersAdded = 0;
    private int collidersAdded = 0;
    private int collidersReplaced = 0;
    private int skipped = 0;

#if UNITY_EDITOR
    [ContextMenu("Run Auto Setup")]
    public void RunAutoSetup()
    {
        Debug.Log("=== GAZE AUTO SETUP START ===");
        Debug.Log($"Preview Mode: {previewMode}");

        ResetStatistics();

        if (setupEnhancedVideoPlayers)
            SetupEnhancedVideoPlayers();

        if (setupUIButtons)
            SetupUIButtons();

        if (setupSpriteRenderers)
            SetupSpriteRenderers();

        if (setupTeleportSigns)
            SetupTeleportSigns();

        PrintStatistics();

        if (!previewMode)
        {
            EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("✓ Changes saved. Remember to save the scene!");
        }
        else
        {
            Debug.Log("ℹ️ Preview mode - no changes made");
        }

        Debug.Log("=== GAZE AUTO SETUP COMPLETE ===");
    }

    private void ResetStatistics()
    {
        enhancedVideoPlayersFound = 0;
        uiButtonsFound = 0;
        spriteRenderersFound = 0;
        teleportSignsFound = 0;
        triggersAdded = 0;
        collidersAdded = 0;
        collidersReplaced = 0;
        skipped = 0;
    }

    private void SetupEnhancedVideoPlayers()
    {
        Debug.Log("--- Setting up EnhancedVideoPlayer objects ---");

        EnhancedVideoPlayer[] players = FindObjectsByType<EnhancedVideoPlayer>(FindObjectsSortMode.None);
        enhancedVideoPlayersFound = players.Length;

        Debug.Log($"Found {players.Length} EnhancedVideoPlayer objects");

        foreach (var player in players)
        {
            if (SetupInteractableObject(player.gameObject, false, worldObjectHoverDelay, true))
            {
                Debug.Log($"✓ Setup: {player.gameObject.name} (EnhancedVideoPlayer)");
            }
        }
    }

    private void SetupUIButtons()
    {
        Debug.Log("--- Setting up UI Buttons ---");

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        uiButtonsFound = buttons.Length;

        Debug.Log($"Found {buttons.Length} UI Button objects");

        foreach (var button in buttons)
        {
            bool isHUD = IsHUDElement(button.gameObject);
            float delay = isHUD ? hudHoverDelay : worldObjectHoverDelay;

            if (SetupInteractableObject(button.gameObject, isHUD, delay, false))
            {
                Debug.Log($"✓ Setup: {button.gameObject.name} (UI Button, HUD: {isHUD})");
            }
        }
    }

    private void SetupSpriteRenderers()
    {
        Debug.Log("--- Setting up SpriteRenderer objects ---");

        SpriteRenderer[] sprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        List<SpriteRenderer> validSprites = new List<SpriteRenderer>();
        foreach (var sprite in sprites)
        {
            if (sprite.GetComponent<EnhancedVideoPlayer>() == null &&
                sprite.GetComponent<Button>() == null)
            {
                validSprites.Add(sprite);
            }
        }

        spriteRenderersFound = validSprites.Count;
        Debug.Log($"Found {validSprites.Count} standalone SpriteRenderer objects");

        foreach (var sprite in validSprites)
        {
            bool isHUD = IsHUDElement(sprite.gameObject);
            float delay = isHUD ? hudHoverDelay : worldObjectHoverDelay;

            if (SetupInteractableObject(sprite.gameObject, isHUD, delay, false))
            {
                Debug.Log($"✓ Setup: {sprite.gameObject.name} (SpriteRenderer, HUD: {isHUD})");
            }
        }
    }

    private void SetupTeleportSigns()
    {
        Debug.Log("--- Setting up Teleport Signs ---");

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> teleportSigns = new List<GameObject>();

        foreach (var obj in allObjects)
        {
            if (IsTeleportSign(obj))
            {
                teleportSigns.Add(obj);
            }
        }

        teleportSignsFound = teleportSigns.Count;
        Debug.Log($"Found {teleportSigns.Count} potential teleport signs");

        foreach (var sign in teleportSigns)
        {
            if (SetupInteractableObject(sign, false, worldObjectHoverDelay, false))
            {
                Debug.Log($"✓ Setup: {sign.name} (Teleport Sign)");
            }
        }
    }

    private bool SetupInteractableObject(GameObject obj, bool isHUD, float hoverDelay, bool forwardToVideoPlayer)
    {
        // Check if already has GazeHoverTrigger
        GazeHoverTrigger existingTrigger = obj.GetComponent<GazeHoverTrigger>();
        if (existingTrigger != null)
        {
            skipped++;
            return false;
        }

        if (previewMode)
        {
            Debug.Log($"[PREVIEW] Would add GazeHoverTrigger to: {obj.name}");
            triggersAdded++;
            return true;
        }

        // FIXED: Handle colliders properly
        EnsureValidTriggerCollider(obj);

        // Add GazeHoverTrigger
        GazeHoverTrigger trigger = obj.AddComponent<GazeHoverTrigger>();

        // Configure trigger
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("actionName").stringValue = obj.name;
        serializedTrigger.FindProperty("hoverDelay").floatValue = hoverDelay;
        serializedTrigger.FindProperty("isHUDElement").boolValue = isHUD;
        serializedTrigger.FindProperty("showCountdown").boolValue = true;
        serializedTrigger.FindProperty("forwardToVideoPlayer").boolValue = forwardToVideoPlayer;
        serializedTrigger.ApplyModifiedProperties();

        triggersAdded++;
        return true;
    }

    /// <summary>
    /// FIXED: Properly handle colliders, especially MeshColliders
    /// </summary>
    private void EnsureValidTriggerCollider(GameObject obj)
    {
        Collider existingCollider = obj.GetComponent<Collider>();

        if (existingCollider == null)
        {
            // No collider - add BoxCollider
            AddBoxCollider(obj);
            collidersAdded++;
            return;
        }

        // Check if it's a MeshCollider
        MeshCollider meshCol = existingCollider as MeshCollider;
        if (meshCol != null)
        {
            // MeshColliders can't be triggers if concave
            if (!meshCol.convex)
            {
                // Replace with BoxCollider
                Debug.Log($"  ⚠️ Replacing concave MeshCollider with BoxCollider on {obj.name}");

                // Get bounds before destroying
                Bounds bounds = meshCol.bounds;
                Vector3 center = bounds.center - obj.transform.position;
                Vector3 size = bounds.size;

                // Destroy mesh collider
                Object.DestroyImmediate(meshCol);

                // Add box collider with same bounds
                BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                boxCol.center = center;
                boxCol.size = size;
                boxCol.isTrigger = true;

                collidersReplaced++;
                return;
            }
            else
            {
                // Convex mesh collider - can be trigger
                meshCol.convex = true;
                meshCol.isTrigger = true;
                return;
            }
        }

        // Other collider types (Box, Sphere, Capsule) - just set isTrigger
        if (!existingCollider.isTrigger)
        {
            existingCollider.isTrigger = true;
        }
    }

    private void AddBoxCollider(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        BoxCollider boxCol = obj.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;

        if (renderer != null)
        {
            // Auto-size to renderer bounds
            Bounds bounds = renderer.bounds;
            boxCol.center = bounds.center - obj.transform.position;
            boxCol.size = bounds.size;
            Debug.Log($"  + Added BoxCollider (auto-sized) to {obj.name}");
        }
        else
        {
            // Default size
            boxCol.size = Vector3.one * 0.5f;
            Debug.Log($"  + Added BoxCollider (default size) to {obj.name}");
        }
    }

    /// <summary>
    /// FIXED: Check tags safely without errors
    /// </summary>
    private bool IsHUDElement(GameObject obj)
    {
        // Check object name
        string objName = obj.name.ToLower();
        foreach (string identifier in hudIdentifiers)
        {
            if (objName.Contains(identifier.ToLower()))
                return true;
        }

        // FIXED: Check tag safely
        try
        {
            if (obj.CompareTag("UI") || obj.CompareTag("HUD"))
                return true;
        }
        catch
        {
            // Tags not defined - ignore silently
        }

        // Check if parent has HUD in name
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            foreach (string identifier in hudIdentifiers)
            {
                if (parentName.Contains(identifier.ToLower()))
                    return true;
            }
            parent = parent.parent;
        }

        return false;
    }

    private bool IsTeleportSign(GameObject obj)
    {
        string objName = obj.name.ToLower();
        foreach (string identifier in teleportIdentifiers)
        {
            if (objName.Contains(identifier.ToLower()))
            {
                if (obj.GetComponent<Renderer>() != null ||
                    obj.GetComponent<SpriteRenderer>() != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void PrintStatistics()
    {
        Debug.Log("=== STATISTICS ===");
        Debug.Log($"EnhancedVideoPlayer objects found: {enhancedVideoPlayersFound}");
        Debug.Log($"UI Button objects found: {uiButtonsFound}");
        Debug.Log($"SpriteRenderer objects found: {spriteRenderersFound}");
        Debug.Log($"Teleport signs found: {teleportSignsFound}");
        Debug.Log($"---");
        Debug.Log($"GazeHoverTrigger components added: {triggersAdded}");
        Debug.Log($"BoxColliders added: {collidersAdded}");
        Debug.Log($"MeshColliders replaced with BoxColliders: {collidersReplaced}");
        Debug.Log($"Skipped (already setup): {skipped}");
        Debug.Log($"---");
        Debug.Log($"TOTAL MODIFIED: {triggersAdded + collidersAdded + collidersReplaced}");
    }
#endif
}

#if UNITY_EDITOR
public static class GazeAutoSetupMenu
{
    [MenuItem("Tools/Gaze System/Auto Setup Current Scene")]
    public static void AutoSetupCurrentScene()
    {
        GameObject setupObj = new GameObject("_GazeAutoSetup");
        GazeAutoSetup setup = setupObj.AddComponent<GazeAutoSetup>();

        setup.setupEnhancedVideoPlayers = true;
        setup.setupUIButtons = true;
        setup.setupSpriteRenderers = true;
        setup.setupTeleportSigns = true;
        setup.previewMode = false;

        setup.RunAutoSetup();

        Object.DestroyImmediate(setupObj);

        Debug.Log("✓ Auto setup complete! Save your scene.");
    }

    [MenuItem("Tools/Gaze System/Preview Auto Setup")]
    public static void PreviewAutoSetup()
    {
        GameObject setupObj = new GameObject("_GazeAutoSetup");
        GazeAutoSetup setup = setupObj.AddComponent<GazeAutoSetup>();

        setup.setupEnhancedVideoPlayers = true;
        setup.setupUIButtons = true;
        setup.setupSpriteRenderers = true;
        setup.setupTeleportSigns = true;
        setup.previewMode = true;

        setup.RunAutoSetup();

        Object.DestroyImmediate(setupObj);

        Debug.Log("ℹ️ Preview complete - no changes made. Run 'Auto Setup Current Scene' to apply.");
    }

    [MenuItem("Tools/Gaze System/Setup Selected Objects Only")]
    public static void SetupSelectedObjects()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected!");
            return;
        }

        int processed = 0;
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.GetComponent<GazeHoverTrigger>() != null)
            {
                Debug.Log($"Skipped {obj.name} - already has GazeHoverTrigger");
                continue;
            }

            // Handle collider
            Collider col = obj.GetComponent<Collider>();
            MeshCollider meshCol = col as MeshCollider;

            if (meshCol != null && !meshCol.convex)
            {
                // Replace with BoxCollider
                Bounds bounds = meshCol.bounds;
                Object.DestroyImmediate(meshCol);

                BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                boxCol.center = bounds.center - obj.transform.position;
                boxCol.size = bounds.size;
                boxCol.isTrigger = true;
                Debug.Log($"+ Replaced MeshCollider with BoxCollider on {obj.name}");
            }
            else if (col == null)
            {
                BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                Debug.Log($"+ Added BoxCollider to {obj.name}");
            }
            else
            {
                col.isTrigger = true;
            }

            // Add trigger
            GazeHoverTrigger trigger = obj.AddComponent<GazeHoverTrigger>();

            bool isHUD = obj.name.ToLower().Contains("hud") ||
                        obj.name.ToLower().Contains("button") ||
                        obj.name.ToLower().Contains("menu");

            bool hasVideoPlayer = obj.GetComponent<EnhancedVideoPlayer>() != null;

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("actionName").stringValue = obj.name;
            serializedTrigger.FindProperty("hoverDelay").floatValue = isHUD ? 2f : 3f;
            serializedTrigger.FindProperty("isHUDElement").boolValue = isHUD;
            serializedTrigger.FindProperty("forwardToVideoPlayer").boolValue = hasVideoPlayer;
            serializedTrigger.ApplyModifiedProperties();

            processed++;
            Debug.Log($"✓ Setup {obj.name}");
        }

        Debug.Log($"=== Setup complete: {processed} objects processed ===");
    }
}
#endif