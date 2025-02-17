using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "DefaultSceneSettings", menuName = "Settings/Default Scene Settings")]
public class DefaultSceneSettings : ScriptableObject
{
    public Vector2 referenceResolution = new Vector2(1080, 1920);
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    public float matchWidthOrHeight = 0.5f;
}
