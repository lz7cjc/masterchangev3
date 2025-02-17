using UnityEditor;
using UnityEngine;

public class TextureFinder : Editor
{
    [MenuItem("Tools/Find All Textures")]
    public static void FindAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Debug.Log("Found texture: " + path);
        }
    }
}
