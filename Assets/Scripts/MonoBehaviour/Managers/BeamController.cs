using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeamController : MonoBehaviour
{
    public static BeamController instance;

    [SerializeField] private int damagePerSecond = 10;
    [SerializeField] private int _intensity = 10;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform beamOriginTransform;
    [SerializeField] private GameObject lineRendererObjectPrefab;

    public List<GameObject> SpawnedLineRenderers { get; private set; } = new();
    private readonly bool enableDebugMousePosition = false;

    #region Instance
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    
    private void FixedUpdate()
    {
        DeleteOldLineRenderers();
        UpdateBeamPath();
    }

    private void DeleteOldLineRenderers()
    {
        while (SpawnedLineRenderers.Count > 0)
        {
            GameObject lrObj = SpawnedLineRenderers[^1];
            SpawnedLineRenderers.RemoveAt(SpawnedLineRenderers.Count - 1);
            Destroy(lrObj);
        }
    }
    
    struct RaycastInfo
    {
        public bool isDarkness;
        public Vector2 contactPoint;
        public Vector2 normal;
        public GateType gateTypeComponent;
        public GameObject hitObject;
    }

    private void UpdateBeamPath()
    {
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector2.zero)
            return;

        Vector2 direction = (mouseWorldPos - (Vector2)beamOriginTransform.position).normalized;

        DrawNextBeam(_intensity + 1, (Vector2)beamOriginTransform.position, direction, null, damagePerSecond);
    }

    private void DrawNextBeam(int intensity, Vector2 origin, Vector2 direction, GameObject ignoreObject, int damagePerSecond)
    {

        RaycastInfo raycastInfo = RaycastForFirstGateTypeOrTheBigDarknessTag(origin, direction, ignoreObject);

        #region Draw the line segment
        LineRenderer segmentLR = Instantiate(lineRendererObjectPrefab, Vector3.zero, Quaternion.identity, beamOriginTransform).GetComponent<LineRenderer>();
        segmentLR.positionCount = 2;
        segmentLR.widthMultiplier = Mathf.Log(intensity);
        segmentLR.SetPosition(0, new(origin.x, origin.y, 0f));
        segmentLR.SetPosition(1, new(raycastInfo.contactPoint.x, raycastInfo.contactPoint.y, 0f));
        SpawnedLineRenderers.Add(segmentLR.gameObject);
        #endregion

        #region Pass on data
        BeamData beamData = segmentLR.GetComponent<BeamData>();
        beamData.damagePerSecond = damagePerSecond;
        #endregion
        
        --intensity;
        if (intensity <= 0)
            return;

        if (raycastInfo.isDarkness)
            return;

        switch (raycastInfo.gateTypeComponent.gateType)
        {
            case GateTypes.Mirror:
                // Reflect the beam
                Vector2 reflectedDir = Vector2.Reflect(direction, raycastInfo.normal);
                DrawNextBeam(intensity, raycastInfo.contactPoint, reflectedDir, raycastInfo.hitObject, damagePerSecond);
                break;
            case GateTypes.Diverging_lens:
                DrawNextBeam(intensity, raycastInfo.contactPoint, direction, raycastInfo.hitObject, damagePerSecond >> 1);
                break;
            case GateTypes.Converging_lens:
                DrawNextBeam(intensity, raycastInfo.contactPoint, direction, raycastInfo.hitObject, damagePerSecond << 1);
                break;

            // TODO: Implement other gate types
            case GateTypes.Lens_system:
                break;
            case GateTypes.One_way_mirror:
                break;
            case GateTypes.Diffraction:
                break;

            default:
                // For other gate types, just stop the beam for now
                Debug.LogWarning($"Gate type {raycastInfo.gateTypeComponent.gateType} not implemented yet.");
                break;
        }
    }

    private RaycastInfo RaycastForFirstGateTypeOrTheBigDarknessTag(Vector2 origin, Vector2 direction, GameObject ignoreObject)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Infinity);
        if (hits != null && hits.Length > 0)
        {
            // Sort hits by distance to make sure we process nearest-first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == ignoreObject) continue;

                // Check for GateType component
                if (hit.collider.TryGetComponent<GateType>(out var gateComp))
                {
                    return new RaycastInfo
                    {
                        isDarkness = false,
                        contactPoint = hit.point,
                        normal = hit.normal,
                        gateTypeComponent = gateComp,
                        hitObject = hit.collider.gameObject
                    };
                }

                // Check for TheBigDarknessTag
                if (hit.collider.TryGetComponent<TheBigDarknessTag>(out var isDarknessTag))
                {
                    return new RaycastInfo
                    {
                        isDarkness = true,
                        contactPoint = hit.point,
                        normal = Vector2.zero,
                        gateTypeComponent = null,
                        hitObject = null
                    };
                }
            }
        }

        // Nothing relevant hit: extend beam to max distance
        return new RaycastInfo
        {
            isDarkness = true,
            contactPoint = Vector2.zero, // arbitrary far point
            normal = Vector2.zero,
            gateTypeComponent = null,
            hitObject = null
        };
    }
    
    
    /// <summary>
    /// Converts mouse screen position to world position for 2D.
    /// Works with both old Input Manager and new Input System.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = GetMouseScreenPosition();
        if (enableDebugMousePosition) Debug.Log($"Mouse Screen Position: {mouseScreenPos}");
        
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    /// <summary>
    /// Gets mouse screen position using the appropriate input system.
    /// </summary>
    private Vector3 GetMouseScreenPosition()
    {
        // New Input System
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        if (Input.mousePosition != null)
        {
            return Input.mousePosition;
        }
        return Vector3.zero;
    }
}
