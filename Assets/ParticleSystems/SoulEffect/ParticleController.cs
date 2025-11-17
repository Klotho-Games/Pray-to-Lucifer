using System.Collections;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    [System.Serializable]
    public class ParticleSystemDelay
    {
        [Tooltip("The particle system to play")]
        public ParticleSystem particleSystem;
        
        [Tooltip("Delay in seconds before playing this particle system")]
        public float delay = 0f;
    }

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystemDelay[] particleSystems;
    [SerializeField] private bool playOnStart = true;

    void Start()
    {
        if (playOnStart)
        {
            PlayAllParticleSystems();
        }
    }

    public void PlayAllParticleSystems()
    {
        if (particleSystems == null || particleSystems.Length == 0)
            return;

        foreach (var psDelay in particleSystems)
        {
            if (psDelay.particleSystem != null)
            {
                StartCoroutine(PlayWithDelay(psDelay.particleSystem, psDelay.delay));
            }
        }
    }

    public void StopAllParticleSystems()
    {
        if (particleSystems == null || particleSystems.Length == 0)
            return;

        foreach (var psDelay in particleSystems)
        {
            if (psDelay.particleSystem != null)
            {
                psDelay.particleSystem.Stop();
            }
        }
    }

    public void PlayParticleSystemAtIndex(int index)
    {
        if (particleSystems == null || index < 0 || index >= particleSystems.Length)
        {
            Debug.LogWarning($"Invalid particle system index: {index}");
            return;
        }

        var psDelay = particleSystems[index];
        if (psDelay.particleSystem != null)
        {
            StartCoroutine(PlayWithDelay(psDelay.particleSystem, psDelay.delay));
        }
    }

    private IEnumerator PlayWithDelay(ParticleSystem ps, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        ps.Play();
    }
}
