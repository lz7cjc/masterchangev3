//using UnityEngine;

///// <summary>
///// GazeController — attach to the Main Camera inside your Cardboard Camera Rig.
///// Raycasts forward every frame and notifies ConstellationOrbs when gaze enters/exits.
///// Works with Google Cardboard SDK (GVR) and standalone VR.
///// </summary>
//public class GazeController : MonoBehaviour
//{
//    [Header("Settings")]
//    [Tooltip("How far the gaze ray reaches")]
//    public float gazeDistance = 20f;

//    [Tooltip("Layer mask — set orbs to an 'Interactable' layer for performance")]
//    public LayerMask interactableLayers = ~0; // Default: everything

//    [Header("Reticle (optional)")]
//    [Tooltip("Assign a reticle GameObject — it will move to the hit point")]
//    public Transform reticle;

//    // ── Private ───────────────────────────────────────────────────────────────
//    private ConstellationOrb _currentOrb;
//    private Vector3 _reticleDefaultPos;

//    void Start()
//    {
//        if (reticle != null)
//            _reticleDefaultPos = new Vector3(0, 0, gazeDistance);
//    }

//    void Update()
//    {
//        Ray ray = new Ray(transform.position, transform.forward);

//        if (Physics.Raycast(ray, out RaycastHit hit, gazeDistance, interactableLayers))
//        {
//            ConstellationOrb orb = hit.collider.GetComponent<ConstellationOrb>();

//            if (orb != null)
//            {
//                // New orb entered
//                if (orb != _currentOrb)
//                {
//                    _currentOrb?.OnGazeExit();
//                    _currentOrb = orb;
//                    _currentOrb.OnGazeEnter();
//                }

//                // Move reticle to hit point
//                if (reticle != null)
//                    reticle.position = hit.point;
//            }
//            else
//            {
//                ClearGaze();
//            }
//        }
//        else
//        {
//            ClearGaze();

//            // Reset reticle to default forward position
//            if (reticle != null)
//                reticle.position = transform.position + transform.forward * gazeDistance;
//        }
//    }

//    private void ClearGaze()
//    {
//        if (_currentOrb != null)
//        {
//            _currentOrb.OnGazeExit();
//            _currentOrb = null;
//        }
//    }
//}
