using UnityEngine;

/// <summary>
/// Rotates the 360 camera (or its rig) using PlayerControlsBridge.LookDelta.
/// Attach ONLY to the object you want to rotate in 360 mode (typically 360Camera).
/// </summary>
public class Camera360LookController : MonoBehaviour
{
    [SerializeField] private PlayerControlsBridge input;

    [Header("Sensitivity (start here)")]
    [SerializeField] private float yawDegreesPerUnit = 0.12f;
    [SerializeField] private float pitchDegreesPerUnit = 0.12f;

    [Header("Pitch Clamp")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        var e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    private void Update()
    {
        if (input == null) return;

        Vector2 d = input.LookDelta;

        yaw += d.x * yawDegreesPerUnit;
        pitch -= d.y * pitchDegreesPerUnit;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
