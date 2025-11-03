using UnityEngine;

/// <summary>
/// Locks the camera at the sphere center - prevents any movement
/// Attach this to your Camera object
/// </summary>
public class LockCameraAtSphereCenter : MonoBehaviour
{
    [SerializeField] private Transform sphereTransform;
    [SerializeField] private bool lockInUpdate = true;
    [SerializeField] private bool lockInLateUpdate = true;

    private Vector3 lockedPosition;

    void Start()
    {
        if (sphereTransform == null)
        {
            // Try to find sphere
            GameObject sphere = GameObject.Find("Sphere");
            if (sphere != null)
            {
                sphereTransform = sphere.transform;
            }
        }

        if (sphereTransform != null)
        {
            lockedPosition = sphereTransform.position;
            transform.position = lockedPosition;
            Debug.Log($"[CAMERA-LOCK] Camera locked at: {lockedPosition}");
        }
        else
        {
            Debug.LogError("[CAMERA-LOCK] Sphere not found! Assign it in Inspector.");
        }
    }

    void Update()
    {
        if (lockInUpdate && sphereTransform != null)
        {
            transform.position = sphereTransform.position;
        }
    }

    void LateUpdate()
    {
        if (lockInLateUpdate && sphereTransform != null)
        {
            // Force position after all other scripts have run
            transform.position = sphereTransform.position;
        }
    }
}