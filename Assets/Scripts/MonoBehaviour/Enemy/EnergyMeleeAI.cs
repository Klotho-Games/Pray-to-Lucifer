using UnityEngine;

public class EnergyMeleeAI : MonoBehaviour
{
    [SerializeField] private EnemySO enemyData;
    public Transform PlayerTransform;
    private Animator animator;
    private PlayerStats playerStats;
    private float attackCooldownTimer = 0f;

    public void Initialize(Transform playerTransform)
    {
        PlayerTransform = playerTransform;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (gameObject.activeInHierarchy == false)
            return;
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        AttackOrMove(distanceToPlayer);
    }

    private void AttackOrMove(float distanceToPlayer)
    {
        bool isInRange = false;
        foreach (var attack in enemyData.attacks)
        {
            if (attack.attackType == EnemySO.AttackType.Melee && distanceToPlayer <= attack.attackRange)
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

    private void PerformAttack(EnemySO.Attack attack)
    {
        if (attackCooldownTimer <= 0f)
        {
            if (playerStats == null)
                playerStats = PlayerTransform.GetComponent<PlayerStats>();

            playerStats.TakeDamage(attack.attackDamage);

            animator.SetTrigger("Attack");

            attackCooldownTimer = attack.attackCooldown;
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private void GoInPlayerDirection()
    {
        if (PlayerTransform == null) return;

        Vector3 direction = (PlayerTransform.position - transform.position).normalized;
        transform.position += enemyData.speed * Time.deltaTime * direction;
    }
}
