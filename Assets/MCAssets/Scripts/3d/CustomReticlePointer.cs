using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CustomReticlePointer : MonoBehaviour
{
    public enum ViewMode { Mode2D, Mode360, ModeVR }

    [Header("Debug Settings")]
    public bool DebugMode = false;

    [Header("Reticle Settings")]
    [Range(-32767, 32767)]
    public int ReticleSortingOrder = 32767;
    public LayerMask ReticleInteractionLayerMask = 1 << _RETICLE_INTERACTION_DEFAULT_LAYER;
    private const int _RETICLE_INTERACTION_DEFAULT_LAYER = 8;
    private const float _RETICLE_MIN_INNER_ANGLE = 0.0f;
    public float _RETICLE_MIN_OUTER_ANGLE = 0.5f;
    public float _RETICLE_GROWTH_ANGLE = 1.5f;
    private const float _RETICLE_MIN_DISTANCE = 0.45f;
    public float _RETICLE_MAX_DISTANCE = 20.0f;
    private const int _RETICLE_SEGMENTS = 20;
    private const float _RETICLE_GROWTH_SPEED = 8.0f;

    [Header("Input Settings")]
    [Tooltip("Name of the trigger action in PlayerInput (leave empty to disable)")]
    public string triggerActionName = "Click"; // Changed from "Trigger" to "Click"

    private GameObject _gazedAtObject = null;
    private Material _reticleMaterial;
    private float _reticleInnerAngle;
    private float _reticleOuterAngle;
    private float _reticleDistanceInMeters;
    private float _reticleInnerDiameter;
    private float _reticleOuterDiameter;

    private PlayerInput playerInput;
    private InputAction triggerAction;
    private ViewMode currentMode = ViewMode.Mode360;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("[CustomReticlePointer] PlayerInput component not found - interaction via input actions will be disabled");
            return;
        }

        // Try to find the trigger action - make it optional
        if (!string.IsNullOrEmpty(triggerActionName))
        {
            try
            {
                triggerAction = playerInput.actions[triggerActionName];
                if (triggerAction != null)
                {
                    Debug.Log($"[CustomReticlePointer] Found trigger action: {triggerActionName}");
                }
                else
                {
                    Debug.LogWarning($"[CustomReticlePointer] Trigger action '{triggerActionName}' not found in PlayerInput actions - click interaction disabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CustomReticlePointer] Could not find trigger action '{triggerActionName}': {e.Message}");
            }
        }
        else
        {
            Debug.Log("[CustomReticlePointer] No trigger action name specified - click interaction disabled");
        }
    }

    private void Start()
    {
        Renderer rendererComponent = GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            rendererComponent.sortingOrder = ReticleSortingOrder;
            _reticleMaterial = rendererComponent.material;
        }
        else
        {
            Debug.LogError("[CustomReticlePointer] Renderer component not found!");
        }

        CreateMesh();
    }

    private void Update()
    {
        // Only do raycasting in VR mode
        if (currentMode != ViewMode.ModeVR)
        {
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, _RETICLE_MAX_DISTANCE, ReticleInteractionLayerMask))
        {
            if (DebugMode)
                Debug.DrawLine(transform.position, hit.point, Color.green);

            if (hit.collider.GetComponent<BoxCollider>() != null)
            {
                if (_gazedAtObject != hit.collider.gameObject)
                {
                    // Exit previous object
                    if (_gazedAtObject != null)
                    {
                        EventTrigger exitTrigger = _gazedAtObject.GetComponent<EventTrigger>();
                        if (exitTrigger != null)
                        {
                            PointerEventData exitData = new PointerEventData(EventSystem.current);
                            ExecuteEvents.Execute(_gazedAtObject, exitData, ExecuteEvents.pointerExitHandler);
                        }
                    }

                    // Enter new object
                    _gazedAtObject = hit.collider.gameObject;
                    EventTrigger enterTrigger = _gazedAtObject.GetComponent<EventTrigger>();
                    if (enterTrigger != null)
                    {
                        PointerEventData enterData = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(_gazedAtObject, enterData, ExecuteEvents.pointerEnterHandler);
                    }
                }

                bool isInteractive = (1 << _gazedAtObject.layer & ReticleInteractionLayerMask) != 0;
                SetParams(hit.distance, isInteractive);
            }
        }
        else
        {
            if (DebugMode)
                Debug.DrawRay(transform.position, transform.forward * _RETICLE_MAX_DISTANCE, Color.red);

            if (_gazedAtObject != null)
            {
                EventTrigger exitTrigger = _gazedAtObject.GetComponent<EventTrigger>();
                if (exitTrigger != null)
                {
                    PointerEventData exitData = new PointerEventData(EventSystem.current);
                    ExecuteEvents.Execute(_gazedAtObject, exitData, ExecuteEvents.pointerExitHandler);
                }
                _gazedAtObject = null;
                ResetParams();
            }
        }

        // Handle click/trigger action if available
        if (triggerAction != null && triggerAction.triggered && _gazedAtObject != null)
        {
            PointerEventData clickData = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(_gazedAtObject, clickData, ExecuteEvents.pointerClickHandler);

            if (DebugMode)
                Debug.Log($"[CustomReticlePointer] Clicked on: {_gazedAtObject.name}");
        }

        UpdateDiameters();
    }

    private void UpdateDiameters()
    {
        if (_reticleMaterial == null) return;

        _reticleDistanceInMeters = Mathf.Clamp(_reticleDistanceInMeters, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);

        if (_reticleInnerAngle < _RETICLE_MIN_INNER_ANGLE)
        {
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        }

        if (_reticleOuterAngle < _RETICLE_MIN_OUTER_ANGLE)
        {
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        }

        float inner_half_angle_radians = Mathf.Deg2Rad * _reticleInnerAngle * 0.5f;
        float outer_half_angle_radians = Mathf.Deg2Rad * _reticleOuterAngle * 0.5f;

        float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
        float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);

        _reticleInnerDiameter = Mathf.Lerp(_reticleInnerDiameter, inner_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        _reticleOuterDiameter = Mathf.Lerp(_reticleOuterDiameter, outer_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);

        _reticleMaterial.SetFloat("_InnerDiameter", _reticleInnerDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat("_OuterDiameter", _reticleOuterDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat("_DistanceInMeters", _reticleDistanceInMeters);
    }

    private void SetParams(float distance, bool interactive)
    {
        _reticleDistanceInMeters = Mathf.Clamp(distance, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);
        if (interactive)
        {
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE + _RETICLE_GROWTH_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE + _RETICLE_GROWTH_ANGLE;
        }
        else
        {
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        }
    }

    private void ResetParams()
    {
        _reticleDistanceInMeters = _RETICLE_MAX_DISTANCE;
        _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        gameObject.AddComponent<MeshFilter>();
        GetComponent<MeshFilter>().mesh = mesh;

        int segments_count = _RETICLE_SEGMENTS;
        int vertex_count = (segments_count + 1) * 2;

        Vector3[] vertices = new Vector3[vertex_count];

        const float kTwoPi = Mathf.PI * 2.0f;
        int vi = 0;
        for (int si = 0; si <= segments_count; ++si)
        {
            float angle = (float)si / (float)segments_count * kTwoPi;

            float x = Mathf.Sin(angle);
            float y = Mathf.Cos(angle);

            vertices[vi++] = new Vector3(x, y, 0.0f);
            vertices[vi++] = new Vector3(x, y, 1.0f);
        }

        int indices_count = (segments_count + 1) * 3 * 2;
        int[] indices = new int[indices_count];

        int vert = 0;
        int idx = 0;
        for (int si = 0; si < segments_count; ++si)
        {
            indices[idx++] = vert + 1;
            indices[idx++] = vert;
            indices[idx++] = vert + 2;

            indices[idx++] = vert + 1;
            indices[idx++] = vert + 2;
            indices[idx++] = vert + 3;

            vert += 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
    }

    public void SetMode(ViewMode newMode)
    {
        currentMode = newMode;

        if (DebugMode)
            Debug.Log($"[CustomReticlePointer] Mode changed to: {newMode}");

        // Hide reticle in non-VR modes
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = (newMode == ViewMode.ModeVR);
        }

        // Clear any gazed object when leaving VR mode
        if (newMode != ViewMode.ModeVR && _gazedAtObject != null)
        {
            EventTrigger exitTrigger = _gazedAtObject.GetComponent<EventTrigger>();
            if (exitTrigger != null)
            {
                PointerEventData exitData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(_gazedAtObject, exitData, ExecuteEvents.pointerExitHandler);
            }
            _gazedAtObject = null;
            ResetParams();
        }
    }

    public ViewMode GetCurrentMode()
    {
        return currentMode;
    }
}