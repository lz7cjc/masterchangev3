using UnityEngine;

/// <summary>
/// Attach to the 3D button prefab (has a collider).
/// Binds to an actionId at runtime and calls HUDMenuController on gaze click.
/// </summary>
public class HudButtonRelay : MonoBehaviour, IGazeClickable
{
    private HUDMenuController controller;
    private string actionId;

    public void Bind(HUDMenuController c, string action)
    {
        controller = c;
        actionId = action;
    }

    public void OnGazeClick()
    {
        if (controller == null) return;
        controller.HandleAction(actionId);
    }
}
