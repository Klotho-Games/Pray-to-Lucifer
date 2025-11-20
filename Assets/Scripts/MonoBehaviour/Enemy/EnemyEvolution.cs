using UnityEngine;

public class EnemyEvolution : MonoBehaviour
{
    enum EnemyType
    {
        EnergyMeleeAI,
        MaterialMeleeAI,
        MaterialProjectileAI,
        MaterialGrenadeAI
    }
    [SerializeField] private GameObject evolvedEnemyPrefab;
    [SerializeField] private float chanceToEvolve = 0.01f;
    [SerializeField] private EnemyType enemyType;

    private void Start()
    {
        TryEvolve();
    }

    public void TryEvolve(float overrideChance = -1f)
    {
        float finalChance = overrideChance >= 0f ? overrideChance : chanceToEvolve;
        if (evolvedEnemyPrefab != null && Random.value < finalChance)
        {
            GameObject evolvedEnemy = Instantiate(evolvedEnemyPrefab, transform.position, transform.rotation, transform.parent);
            switch (enemyType)
            {
                case EnemyType.EnergyMeleeAI:
                    if (evolvedEnemy.TryGetComponent(out EnergyMeleeAI energyMeleeAI) && TryGetComponent<EnergyMeleeAI>(out EnergyMeleeAI energyMeleeAIOriginal))
                        energyMeleeAI.Initialize(energyMeleeAIOriginal.PlayerTransform);
                    break;
                case EnemyType.MaterialMeleeAI:
                    if (evolvedEnemy.TryGetComponent(out MaterialMeleeAI materialMeleeAI) && TryGetComponent<MaterialMeleeAI>(out MaterialMeleeAI materialMeleeAIOriginal))
                        materialMeleeAI.Initialize(materialMeleeAIOriginal.PlayerTransform);
                    break;
                case EnemyType.MaterialProjectileAI:
                    if (evolvedEnemy.TryGetComponent(out MaterialProjectileAI materialProjectileAI) && TryGetComponent<MaterialProjectileAI>(out MaterialProjectileAI materialProjectileAIOriginal))
                        materialProjectileAI.Initialize(materialProjectileAIOriginal.PlayerTransform);
                    break;
                case EnemyType.MaterialGrenadeAI:
                    if (evolvedEnemy.TryGetComponent(out MaterialGrenadeAI materialGrenadeAI) && TryGetComponent<MaterialGrenadeAI>(out MaterialGrenadeAI materialGrenadeAIOriginal))
                        materialGrenadeAI.Initialize(materialGrenadeAIOriginal.PlayerTransform);
                    break;
                default:
                    Debug.LogError("Unrecognized enemy type for evolution.");
                    break;
            }
            Destroy(gameObject);
        }
    }
}
