using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamController : MonoBehaviour
{
    public static BeamController instance;

    [SerializeField] private int _damagePerSecond = 10;
    [SerializeField] private int _intensity = 10;
    [SerializeField] private float beamConeAngle = 45f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform beamOriginTransform;
    [SerializeField] private GameObject lineRendererObjectPrefab;
    [SerializeField] private PlayerSoulState playerSoulState;
    [SerializeField] private Animator animator;
    
    [Header("Beam Wave Settings")]
    [SerializeField] private bool enableWave = true;
    [SerializeField] private float amplitude = 0.1f;
    [SerializeField] private float wavelength = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private int numberOfSegments = 20;
    
    [Header("Particle & Light Effects")]
    [SerializeField] private GameObject particleSystemPrefab;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableLights = true;
    [SerializeField] private float lightIntensityMultiplier = 0.5f;
    [SerializeField] private Color lightColor = new(1f, 0.3f, 0.1f, 1f);

    public List<LineRenderer> SpawnedLineRenderers { get; private set; } = new();
    private readonly List<GameObject> spawnedEffects = new();

    private Vector2 facingDirection;
    private Vector2 lastBeamDirection = Vector2.zero;
    private int spawnedLineRenderersNextToRedrawCache = 0;

    private readonly float[][] diffractionAngles = new float[][]
    {
        new float[] { 45.7f },
        new float[] { 55.1f, 28.5f },
        new float[] { 60.2f, 37.9f, 21f },
        new float[] { 63.6f, 44f, 29.5f, 16.6f },
        new float[] { 66f, 48.3f, 35.3f, 24.2f, 13.8f }
    };

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

    void Start()
    {
        FacingDirectionUpdate();
    }

    private void FixedUpdate()
    {
        UpdateBeamPath();
    }

    private int Increase(int value)
    {
        return value << 1;
    }
    
    private int Decrease(int value)
    {
        return value >> 1;
    }

    private void DestroyOldLineRenderers()
    {
        foreach (var lr in SpawnedLineRenderers)
        {
            Destroy(lr.gameObject);
        }
        SpawnedLineRenderers.Clear();
        
        foreach (var obj in spawnedEffects)
        {
            Destroy(obj);
        }
        spawnedEffects.Clear();
    }
    
    struct RaycastInfo
    {
        public bool isDarkness;
        public Vector2 contactPoint;
        public Vector2 normal;
        public GateType gateTypeComponent;
        public GameObject hitObject;
    }

    void OnDisable()
    {
        if (beamOriginTransform != null && beamOriginTransform.gameObject != null)
        {
            beamOriginTransform.gameObject.SetActive(false);
            DestroyOldLineRenderers();
        }
    }

    private bool BeamOriginIsAllGood()
    {
        if (beamOriginTransform.gameObject == null)
            return false;
        if (beamOriginTransform.gameObject.activeSelf == false)
            beamOriginTransform.gameObject.SetActive(true);
        if (!beamOriginTransform.gameObject.activeInHierarchy)
            return false;
        return true;
    }

    private void FacingDirectionUpdate()
    {
        if (animator.GetInteger("isShootingWhileMovingInDirection") != -1)
        {
            int dirIndex = animator.GetInteger("isShootingWhileMovingInDirection");
            facingDirection = DirectionFromIndex(dirIndex);
        }
        else if (animator.GetInteger("isShootingFacing") != -1)
        {
            int dirIndex = animator.GetInteger("isShootingFacing") * 2;
            facingDirection = DirectionFromIndex(dirIndex);
        }

        static Vector2 DirectionFromIndex(int index)
        {
            return index switch
            {
                0 => Vector2.down,
                1 => new Vector2(1, -1).normalized,
                2 => Vector2.right,
                3 => new Vector2(1, 1).normalized,
                4 => Vector2.up,
                5 => new Vector2(-1, 1).normalized,
                6 => Vector2.left,
                7 => new Vector2(-1, -1).normalized,
                _ => Vector2.up,
            };
        }
    }

    private void UpdateBeamPath()
    {
        spawnedLineRenderersNextToRedrawCache = 0;
        StartCoroutine(DestroyAdditionalLineRenderersAndEffectsAtTheEndOfFrame());

        if (!BeamOriginIsAllGood())
            return;

        Vector2 direction = (InputManager.instance.IsKeyboardAndMouse ? (InputManager.instance.MousePosition - (Vector2)beamOriginTransform.position) : InputManager.instance.RightStick).normalized;
        if (direction == Vector2.zero)
        {
            if (lastBeamDirection != Vector2.zero)
                direction = lastBeamDirection;
            else
            {
                FacingDirectionUpdate();
                direction = facingDirection; // fallback to direction player is facing
            }
        }
        else
        {
            lastBeamDirection = direction;
        }

        DrawNextBeam(_intensity + 1, (Vector2)beamOriginTransform.position, playerSoulState.currentSoulState is null ? TranslateDirection(direction) : direction, null, _damagePerSecond);

        Vector2 TranslateDirection(Vector2 direction)
        {
            FacingDirectionUpdate();

            if (!InputManager.instance.PreciseControlInput)
                return facingDirection;

            //Trim to 90 degree cone around facingDirection
            float angleBetween = Vector2.Angle(direction, facingDirection);
            if (angleBetween <= beamConeAngle)
                return direction;
            
            float sign = Mathf.Sign(Vector3.Cross(facingDirection, direction).z);
            float clampedAngle = beamConeAngle * sign;
            Vector2 clampedDirection = Quaternion.Euler(0, 0, clampedAngle) * facingDirection;
            float mirroredAngle = beamConeAngle - Vector2.Angle(direction, clampedDirection); 
            return Quaternion.Euler(0, 0, mirroredAngle * sign) * facingDirection;
        }
    }

    private IEnumerator DestroyAdditionalLineRenderersAndEffectsAtTheEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        while (spawnedLineRenderersNextToRedrawCache < SpawnedLineRenderers.Count)
        {
            Destroy(SpawnedLineRenderers[spawnedLineRenderersNextToRedrawCache].gameObject);
            SpawnedLineRenderers.RemoveAt(spawnedLineRenderersNextToRedrawCache);
            if (spawnedEffects.Count > spawnedLineRenderersNextToRedrawCache)
            {
                Destroy(spawnedEffects[spawnedLineRenderersNextToRedrawCache]);
                spawnedEffects.RemoveAt(spawnedLineRenderersNextToRedrawCache);
            }
            spawnedLineRenderersNextToRedrawCache++;
        }
    }

    private void DrawNextBeam(int intensity, Vector2 origin, Vector2 direction, GameObject ignoreObject, int damagePerSecond)
    {
        if (!BeamOriginIsAllGood())
            return;

        RaycastInfo raycastInfo = RaycastForFirstGateTypeOrTheBigDarknessTag(origin, direction, ignoreObject);

        #region Draw the line segment
        LineRenderer segmentLR;
        if (spawnedLineRenderersNextToRedrawCache < SpawnedLineRenderers.Count)
        {
            segmentLR = SpawnedLineRenderers[spawnedLineRenderersNextToRedrawCache];
        }
        else
        {
            segmentLR = Instantiate(lineRendererObjectPrefab, Vector3.zero, Quaternion.identity, beamOriginTransform).GetComponent<LineRenderer>();
            SpawnedLineRenderers.Add(segmentLR);
        }
        ++spawnedLineRenderersNextToRedrawCache;
        
        #region Pass on data to the new beam segment
        BeamData beamData = segmentLR.GetComponent<BeamData>();
        beamData.damagePerSecond = damagePerSecond;
        #endregion

        if (enableWave)
        {
            ApplyWaveToLineRenderer(segmentLR, origin, raycastInfo.contactPoint, damagePerSecond);
        }
        else
        {
            segmentLR.positionCount = 2;
            segmentLR.SetPosition(0, new(origin.x, origin.y, 0f));
            segmentLR.SetPosition(1, new(raycastInfo.contactPoint.x, raycastInfo.contactPoint.y, 0f));
        }
        
        segmentLR.widthMultiplier = Mathf.Log(intensity);
        #endregion
        
        #region Spawn particle trail along beam
        if ((enableParticles || enableLights) && particleSystemPrefab != null)
        {
            SpawnBeamEffects(origin, raycastInfo.contactPoint, intensity);
        }
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
                // Decrease the dmg of the beam
                DrawNextBeam(intensity, raycastInfo.contactPoint, direction, raycastInfo.hitObject, Decrease(damagePerSecond));
                break;
            case GateTypes.Converging_lens:
                // Increase the dmg of the beam
                DrawNextBeam(intensity, raycastInfo.contactPoint, direction, raycastInfo.hitObject, Increase(damagePerSecond));
                break;
            case GateTypes.Diffraction:
                // pass the beam straight through
                DrawNextBeam(intensity, raycastInfo.contactPoint, direction, raycastInfo.hitObject, damagePerSecond);

                for (int i = 0; i < raycastInfo.gateTypeComponent.gateLevel; ++i)
                {
                    float angleOffsetDeg = diffractionAngles[raycastInfo.gateTypeComponent.gateLevel - 1][i];
                    int decreasedDMG = damagePerSecond;
                    for (int j = 0; j < i; ++j)
                    {
                        decreasedDMG = Decrease(decreasedDMG);
                    }
                    // Right side
                    Vector2 rotatedDirLeft = Quaternion.Euler(0, 0, -angleOffsetDeg) * direction;
                    // Left side                        
                    Vector2 rotatedDirRight = Quaternion.Euler(0, 0, angleOffsetDeg) * direction;
                    // Don't draw beams that would cross back through the gate
                    if (VectorShadowsHaveSameTurn(raycastInfo.normal, direction, rotatedDirRight))
                    {
                        DrawNextBeam(intensity, raycastInfo.contactPoint, rotatedDirRight, raycastInfo.hitObject, decreasedDMG);
                    }
                    if (VectorShadowsHaveSameTurn(raycastInfo.normal, direction, rotatedDirLeft))
                    {
                        DrawNextBeam(intensity, raycastInfo.contactPoint, rotatedDirLeft, raycastInfo.hitObject, decreasedDMG);
                    }
                }
                break;

            case GateTypes.One_way_mirror:
                break;

            default:
                // For other gate types, just stop the beam for now
                Debug.LogWarning($"Gate type {raycastInfo.gateTypeComponent.gateType} not implemented yet.");
                break;
        }
    }

    bool VectorShadowsHaveSameTurn(Vector2 axis, Vector2 vec1, Vector2 vec2)
    {
        return (Vector2.Dot(vec1, axis) * Vector2.Dot(vec2, axis)) > 0;
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
    
    private void SpawnBeamEffects(Vector2 startPos, Vector2 endPos, int intensity)
    {
        Vector2 direction = (endPos - startPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float beamLength = Vector2.Distance(startPos, endPos);
        
        // Position at start point, then offset half the light radius along the beam direction
        Vector2 lightPosition = startPos + direction * (beamLength * 0.5f);
        
        GameObject effectObj;
        int effectIndex = spawnedLineRenderersNextToRedrawCache - 1; // Effect index matches beam segment index
        
        // Reuse or create effect object
        if (effectIndex < spawnedEffects.Count)
        {
            effectObj = spawnedEffects[effectIndex];
            effectObj.transform.SetPositionAndRotation(new Vector3(lightPosition.x, lightPosition.y, 0f), Quaternion.Euler(0, 0, angle));
        }
        else
        {
            // Create particle system or empty GameObject
            if (enableParticles && particleSystemPrefab != null)
            {
                effectObj = Instantiate(particleSystemPrefab, new Vector3(lightPosition.x, lightPosition.y, 0f), Quaternion.Euler(0, 0, angle), beamOriginTransform);
            }
            else
            {
                effectObj = new GameObject("BeamLight");
                effectObj.transform.SetParent(beamOriginTransform);
                effectObj.transform.SetPositionAndRotation((Vector3)lightPosition, Quaternion.Euler(0, 0, angle));
            }
            spawnedEffects.Add(effectObj);
        }
        
        // Update particle system if present
        if (enableParticles && effectObj.TryGetComponent<ParticleSystem>(out var ps))
        {
            var shape = ps.shape;
            shape.scale = new Vector3(beamLength, 1f, 1f);
            
            var emission = ps.emission;
            emission.enabled = false;
            
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        
        // Get or add Light2D component (don't stack multiple lights)
        if (enableLights)
        {
            if (!effectObj.TryGetComponent<UnityEngine.Rendering.Universal.Light2D>(out var light2D))
            {
                light2D = effectObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                light2D.color = lightColor;
            }
            
            light2D.intensity = Mathf.Log(intensity + 1) * lightIntensityMultiplier;
            light2D.pointLightOuterRadius = beamLength * 0.5f;
            light2D.pointLightInnerRadius = 0f;
        }
    }
    
    private void ApplyWaveToLineRenderer(LineRenderer lr, Vector2 startPos, Vector2 endPos, int damagePerSecondOfSegment)
    {
        float beamLength = Vector2.Distance(startPos, endPos);
        Vector2 direction = (endPos - startPos).normalized;
        Vector2 perpendicular = new(-direction.y, direction.x); // Perpendicular vector for offset
        
        // Calculate segments based on beam length to maintain consistent wave sampling
        int segments = Mathf.Max(2, Mathf.CeilToInt(beamLength / (wavelength * _damagePerSecond / damagePerSecondOfSegment / numberOfSegments)));
        lr.positionCount = segments + 1;
        
        float k = 2f * Mathf.PI / (wavelength * damagePerSecondOfSegment / _damagePerSecond);
        float omega = 2f * Mathf.PI * frequency;
        float time = Time.time;
        
        // Calculate step size in world units for consistent wave pattern
        float stepSize = beamLength / segments;
        
        lr.SetPosition(0, new(startPos.x, startPos.y, 0f));
        
        for (int i = 1; i <= segments; i++)
        {
            float distanceAlongBeam = i * stepSize; // Absolute distance from start
            Vector2 basePosition = startPos + direction * distanceAlongBeam;
            
            // Calculate sine wave offset using absolute distance
            float sineValue = amplitude * Mathf.Sin(k * distanceAlongBeam - omega * time);
            Vector2 offset = perpendicular * sineValue;
            
            Vector2 finalPosition = basePosition + offset;
            lr.SetPosition(i, finalPosition);
        }
    }
}
