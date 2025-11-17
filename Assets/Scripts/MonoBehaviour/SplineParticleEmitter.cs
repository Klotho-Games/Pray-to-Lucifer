using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class SplineParticleEmitter : MonoBehaviour
{
    [Header("Spline Settings")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private bool isStatic = true;
    [SerializeField] private int splineSampleResolution = 100;
    
    [Header("Particle Settings")]
    [SerializeField] private int particlesPerSecond = 50;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private int maxParticles = 200;
    [SerializeField] private int maxSamplingAttempts = 30;
    
    private ParticleSystem splineParticleSystem;
    private ParticleSystem.Particle[] particles;
    private float emissionTimer = 0f;
    private Bounds splineBounds;
    private List<Vector3> cachedSplinePolygon = null;
    
    void Start()
    {
        splineParticleSystem = GetComponent<ParticleSystem>();
        
        // Configure particle system
        var main = splineParticleSystem.main;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = splineParticleSystem.emission;
        emission.enabled = false; // We'll emit manually
        
        var velocity = splineParticleSystem.velocityOverLifetime;
        velocity.enabled = false;
        
        if (splineContainer != null && splineContainer.Spline != null)
        {
            CalculateSplineBounds();
            
            if (isStatic)
            {
                cachedSplinePolygon = SampleSplineToPolygon();
            }
        }
        
        particles = new ParticleSystem.Particle[maxParticles];
    }
    
    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;
        
        // Emit particles
        emissionTimer += Time.deltaTime;
        float interval = 1f / particlesPerSecond;
        
        while (emissionTimer >= interval)
        {
            emissionTimer -= interval;
            EmitParticleInSpline();
        }
        
        // Update particle colors based on local X position
        UpdateParticleColors();
    }
    
    private void CalculateSplineBounds()
    {
        splineBounds = new Bounds();
        bool first = true;
        
        // Sample spline to find bounds
        for (float t = 0; t <= 1f; t += 0.01f)
        {
            Vector3 worldPos = splineContainer.transform.TransformPoint(
                splineContainer.Spline.EvaluatePosition(t)
            );
            
            if (first)
            {
                splineBounds = new Bounds(worldPos, Vector3.zero);
                first = false;
            }
            else
            {
                splineBounds.Encapsulate(worldPos);
            }
        }
    }
    
    private void EmitParticleInSpline()
    {
        Vector3 randomPoint;
        int attempts = 0;
        
        // Try to find a point inside the spline
        do
        {
            randomPoint = new Vector3(
                Random.Range(splineBounds.min.x, splineBounds.max.x),
                Random.Range(splineBounds.min.y, splineBounds.max.y),
                splineBounds.center.z
            );
            attempts++;
        } while (!IsPointInsideSpline(randomPoint) && attempts < maxSamplingAttempts);
        
        if (attempts >= maxSamplingAttempts)
            return; // Failed to find valid point
        
        // Emit particle at this position
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = randomPoint;
        emitParams.velocity = Vector3.zero;
        emitParams.startColor = GetColorForPosition(randomPoint);
        
        splineParticleSystem.Emit(emitParams, 1);
    }
    
    private List<Vector3> SampleSplineToPolygon()
    {
        List<Vector3> polygon = new List<Vector3>();
        
        // Sample the spline at high resolution to approximate curves
        for (int i = 0; i < splineSampleResolution; i++)
        {
            float t = (float)i / splineSampleResolution;
            Vector3 localPos = splineContainer.Spline.EvaluatePosition(t);
            polygon.Add(localPos);
        }
        
        return polygon;
    }
    
    private bool IsPointInsideSpline(Vector3 worldPoint)
    {
        if (splineContainer == null || splineContainer.Spline == null || !splineContainer.Spline.Closed)
            return false;
        
        // Convert to local space
        Vector3 localPoint = splineContainer.transform.InverseTransformPoint(worldPoint);
        
        // Get polygon points (cached if static, or calculate on-the-fly if dynamic)
        List<Vector3> polygon = isStatic ? cachedSplinePolygon : SampleSplineToPolygon();
        
        if (polygon == null || polygon.Count < 3)
            return false;
        
        // Ray casting method for point-in-polygon test
        int intersections = 0;
        
        // Cast ray to the right and count intersections with polygon edges
        for (int i = 0; i < polygon.Count; i++)
        {
            int nextIndex = (i + 1) % polygon.Count;
            
            Vector3 p1 = polygon[i];
            Vector3 p2 = polygon[nextIndex];
            
            // Check intersection with horizontal ray
            if (RayIntersectsSegment(localPoint, p1, p2))
            {
                intersections++;
            }
        }
        
        // Odd number of intersections means inside
        return (intersections % 2) == 1;
    }
    
    private bool RayIntersectsSegment(Vector3 rayOrigin, Vector3 segmentStart, Vector3 segmentEnd)
    {
        // Horizontal ray to the right
        if ((segmentStart.y > rayOrigin.y) == (segmentEnd.y > rayOrigin.y))
            return false;
        
        float t = (rayOrigin.y - segmentStart.y) / (segmentEnd.y - segmentStart.y);
        float intersectionX = segmentStart.x + t * (segmentEnd.x - segmentStart.x);
        
        return intersectionX > rayOrigin.x;
    }
    
    private void UpdateParticleColors()
    {
        int numParticles = splineParticleSystem.GetParticles(particles);
        
        for (int i = 0; i < numParticles; i++)
        {
            particles[i].startColor = GetColorForPosition(particles[i].position);
        }
        
        splineParticleSystem.SetParticles(particles, numParticles);
    }
    
    private Color GetColorForPosition(Vector3 worldPosition)
    {
        // Normalize X position within spline bounds to 0-1 range
        float normalizedX = Mathf.InverseLerp(splineBounds.min.x, splineBounds.max.x, worldPosition.x);
        return colorGradient.Evaluate(normalizedX);
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineContainer != null && splineContainer.Spline != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
            
            // Draw the sampled polygon for debugging
            if (Application.isPlaying && cachedSplinePolygon != null && cachedSplinePolygon.Count > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < cachedSplinePolygon.Count; i++)
                {
                    int nextIndex = (i + 1) % cachedSplinePolygon.Count;
                    Vector3 p1 = splineContainer.transform.TransformPoint(cachedSplinePolygon[i]);
                    Vector3 p2 = splineContainer.transform.TransformPoint(cachedSplinePolygon[nextIndex]);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
    }
}
