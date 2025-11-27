using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private EnemySO enemyData;
    [SerializeField] private CircleCollider2D attackArea;

    private EnemySO.Attack attackInfo;
    private Collider2D playerColl;
    private PlayerStats playerStats;
    private float cooldownTimer = 0f;

    public void Initialize(Collider2D pColl, PlayerStats pStats)
    {
        playerColl = pColl;
        playerStats = pStats;
        
        foreach (var attack in enemyData.Attacks)
        {
            if (attack.Type == EnemySO.AttackType.Melee)
            {
                attackInfo = attack;
                break;
            }
        }
        if (attackInfo == null)
        {
            throw new System.Exception("No melee attack found in EnemySO for MeleeAttack component.");
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other == playerColl)
        {
            if (cooldownTimer <= 0f)
            {
                StartCoroutine(DelayedAttack());
                cooldownTimer = attackInfo.Cooldown;
            }
        }
    }

    private IEnumerator DelayedAttack()
    {
        // start animation here
        yield return new WaitForSeconds(attackInfo.DelayAfterTrigger);
        playerStats.TakeDamage(attackInfo.Damage);
    }
}
