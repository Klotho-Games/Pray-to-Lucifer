using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class SplineParticleEmitter : MonoBehaviour
{
    [Header("Spline Settings")]
    [SerializeField] private SplineContainer splineContainer;
    
    [Header("Particle Settings")]
    [SerializeField] private int particlesPerSecond = 50;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private int maxParticles = 200;
    [SerializeField] private int maxSamplingAttempts = 30;
    
    private ParticleSystem splineParticleSystem;
    private ParticleSystem.Particle[] particles;
    private float emissionTimer = 0f;
    private Bounds splineBounds;
    
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
    
    private bool IsPointInsideSpline(Vector3 worldPoint)
    {
        if (splineContainer == null || splineContainer.Spline == null || !splineContainer.Spline.Closed)
            return false;
        
        // Convert to local space
        Vector3 localPoint = splineContainer.transform.InverseTransformPoint(worldPoint);
        
        // Ray casting method for point-in-polygon test
        int intersections = 0;
        float rayLength = splineBounds.size.x * 2f;
        
        // Cast ray to the right and count intersections
        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            int nextIndex = (i + 1) % splineContainer.Spline.Count;
            
            // Sample segment
            Vector3 p1 = splineContainer.Spline.EvaluatePosition((float)i / splineContainer.Spline.Count);
            Vector3 p2 = splineContainer.Spline.EvaluatePosition((float)nextIndex / splineContainer.Spline.Count);
            
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
        }
    }
}
