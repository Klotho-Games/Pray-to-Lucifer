using UnityEngine;

public class EnemyLifeSystem : MonoBehaviour
{
    [SerializeField] private EnemySO enemyData;
    [SerializeField] private Collider2D coll;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float redColorPercent = 0.6f; // Percent HP at which color is fully red
    [SerializeField] private Color originalColor;
    [SerializeField] private GameObject soulShardPrefab;
    [SerializeField] private bool isTutorialEnemy = false;
    
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 100;

    private int currentHP;

    private float averageDamageThisSecond = 0f;
    private float timeSinceLastDamage = 0f;
    private float timeSinceLastRegeneration = 0f;

    public void ResetHealth()
    {
        currentHP = enemyData.MaxHP;
        averageDamageThisSecond = 0f;
        timeSinceLastDamage = 0f;
        timeSinceLastRegeneration = 0f;
        spriteRenderer.color = originalColor;
    }

    void Awake()
    {
        if (coll == null)
        {
            coll = GetComponent<Collider2D>();
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
    }

    void Start()
    {
        if (enemyData == null)
        {
            new System.Exception("Enemy data is not assigned in EnemyLifeSystem.");
        }

        originalColor = spriteRenderer.color;
        ResetHealth();
    }

    void Update()
    {
        if (spriteRenderer.color.a == 0f)
        {
            Debug.LogWarning("Enemy sprite is invisible. Resetting alpha to 1.");
            var newColor = spriteRenderer.color;
            newColor.a = 1f;
            spriteRenderer.color = newColor;
        }

        if (!GetDamaged())
        {
            RegenerateHealth();
            timeSinceLastDamage = 0f;
        }
        else
            timeSinceLastRegeneration = 0f;

        ChangeColor();
    }

    private void Die()
    {
        // Handle enemy death (e.g., play animation, drop rewards)
        SFXManager.instance.PlayEnemyDeathSFX(enemyData.Name, transform.position);
        SpawnSoulShard();

        if (isTutorialEnemy)
        {
            gameObject.SetActive(false);
            gameObject.transform.parent = null;
            return;
        }

        ResetHealth();
        ObjectPooler.instance.ReturnToPool(gameObject, gameObject);
    }

    private void SpawnSoulShard()
    {
        if (soulShardPrefab != null)
        {
            SoulShard shard = ObjectPooler.instance.GetFromPool(soulShardPrefab, transform.position, transform.parent, poolSize).GetComponent<SoulShard>();
            shard.Initialize(enemyData.SoulRewardAmount);
        }
    }

    private bool GetDamaged()
    {
        if (BeamController.instance.IsBeamActive == false)
            return false;
        
        bool takingDamage = false;

        foreach (LineRenderer beamLR in BeamController.instance.SpawnedLineRenderers)
        {
            if (!beamLR.TryGetComponent<BeamData>(out var beamData)) continue;
            if (!DoesLineIntersectCollider(beamLR))
                continue;

            GetDamagedFromLine(beamData.damagePerSecond);
            
            takingDamage = true;
        }


        return takingDamage;
        
        void GetDamagedFromLine(int damagePerSecond)
        {
            int damageTaken = currentHP;
            currentHP -= Mathf.FloorToInt(averageDamageThisSecond * (Time.deltaTime + timeSinceLastDamage));
            currentHP = Mathf.Max(currentHP, 0);
            damageTaken -= currentHP;

            if (damageTaken > 0)
            {
                timeSinceLastDamage = 0f;
                // Optional: Handle damage effects
            }
            else
            {
                averageDamageThisSecond = (timeSinceLastDamage*averageDamageThisSecond + damagePerSecond * Time.deltaTime) / (timeSinceLastDamage + Time.deltaTime);
                timeSinceLastDamage += Time.deltaTime;

                if (currentHP <= 0)
                {
                    Die();
                }
            }
        }
    }
    
    private bool DoesLineIntersectCollider(LineRenderer lr)
    {
        // Check all segments of the LineRenderer
        int posCount = lr.positionCount;
        if (posCount < 2) return false;
        
        for (int i = 0; i < posCount - 1; i++)
        {
            Vector3 segmentStart = lr.GetPosition(i);
            Vector3 segmentEnd = lr.GetPosition(i + 1);
            
            RaycastHit2D[] hits = Physics2D.LinecastAll(segmentStart, segmentEnd);
            
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == coll)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private void RegenerateHealth()
    {
        if (currentHP < enemyData.MaxHP)
        {
            int amountRegenerated = currentHP;
            currentHP += Mathf.FloorToInt(enemyData.Regeneration * (Time.deltaTime + timeSinceLastRegeneration));
            currentHP = Mathf.Min(currentHP, enemyData.MaxHP);
            amountRegenerated = currentHP - amountRegenerated;

            if (amountRegenerated > 0)
            {
                timeSinceLastRegeneration = 0f;
                // Optional: Handle regeneration effects
            }
            else
            {
                timeSinceLastRegeneration += Time.deltaTime;

                if (currentHP == enemyData.MaxHP)
                {
                    // Optional: Handle fully regenerated effects
                }
            }
        }
    }

    private void ChangeColor()
    {
        float hpPercent = (float)currentHP / enemyData.MaxHP;
        
        // Gradient: originalColor (100% HP) -> red (redColorPercent HP) -> white (0% HP)
        if (hpPercent > redColorPercent)
        {
            // Lerp from originalColor to red (100% to redColorPercent HP)
            float t = (hpPercent - redColorPercent) / (1f - redColorPercent); // Remap redColorPercent-1.0 to 0.0-1.0
            spriteRenderer.color = Color.Lerp(Color.red, originalColor, t);
        }
        else
        {
            // Lerp from red to white (redColorPercent to 0% HP)
            float t = hpPercent / redColorPercent; // Remap 0.0-redColorPercent to 0.0-1.0
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, t);
        }
    }
}
