using UnityEngine;

public class EnergyMeleeAI : MonoBehaviour
{
    [SerializeField] private Enemy enemyData;
    private Transform player;
    private PlayerStats playerStats;
    private float attackCooldownTimer = 0f;

    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        AttackOrMove(distanceToPlayer);
    }

    private void AttackOrMove(float distanceToPlayer)
    {
        bool isInRange = false;
        foreach (var attack in enemyData.attacks)
        {
            if (attack.attackType == Enemy.AttackType.Melee && distanceToPlayer <= attack.attackRange)
            {
                PerformAttack(attack);
                isInRange = true;
            }
        }

        if (!isInRange)
        {
            GoInPlayerDirection();
        }
    }

    private void PerformAttack(Enemy.Attack attack)
    {
        if (attackCooldownTimer <= 0f)
        {
            playerStats ??= player.GetComponent<PlayerStats>();
            playerStats.TakeDamage(attack.attackDamage);

            attackCooldownTimer = attack.attackCooldown;
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private void GoInPlayerDirection()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += enemyData.speed * Time.deltaTime * direction;
    }
}
