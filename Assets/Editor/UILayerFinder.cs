using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class UILayerFinder : EditorWindow
{
    private Vector2 scrollPosition;
    private bool includeInactive = true;
    private string searchFilter = "";
    private List<GameObject> uiObjects = new List<GameObject>();
    private LayerMask uiLayer;

    [MenuItem("Tools/UI Layer Finder")]
    public static void ShowWindow()
    {
        GetWindow<UILayerFinder>("UI Layer Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI Layer Object Finder", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Settings
        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
        searchFilter = EditorGUILayout.TextField("Search Filter", searchFilter);

        if (GUILayout.Button("Find UI Layer Objects"))
        {
            FindUIObjects();
        }

        EditorGUILayout.Space();

        // Display results
        if (uiObjects.Count > 0)
        {
            GUILayout.Label($"Found {uiObjects.Count} objects in UI layer:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (GameObject obj in uiObjects)
            {
                if (obj == null) continue;

                // Apply search filter
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !obj.name.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginHorizontal();

                // Object field that can be clicked to select
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);

                // Select button
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = obj;
                    SceneView.FrameLastActiveSceneView();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void FindUIObjects()
    {
        uiObjects.Clear();
        uiLayer = LayerMask.NameToLayer("UI");

        GameObject[] allObjects = includeInactive ?
            Resources.FindObjectsOfTypeAll<GameObject>() :
            Object.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == uiLayer)
            {
                // Only include scene objects, not prefabs
                if (PrefabUtility.GetPrefabInstanceStatus(obj) != PrefabInstanceStatus.NotAPrefab)
                    continue;

                uiObjects.Add(obj);
            }
        }
    }
}
#endif