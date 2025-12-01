using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private EnemySO enemyData;
    [SerializeField] private CircleCollider2D attackArea;
    [SerializeField] private Animator animator;

    private EnemySO.Attack attackInfo;
    private Collider2D playerColl;
    private PlayerStats playerStats;
    private float cooldownTimer = 0f;
    private bool playerWithinRange = false;
    private bool isAttacking = false;

    private void OnDisable()
    {
        isAttacking = false;
        cooldownTimer = 0f;
        playerWithinRange = false;
    }

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

        if (playerWithinRange && cooldownTimer <= 0f && !isAttacking)
        {
            StartCoroutine(DelayedAttack());
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other != playerColl)
            return;

        playerWithinRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other != playerColl)
            return;

        playerWithinRange = false;
    }

    private IEnumerator DelayedAttack()
    {
        if (isAttacking)
            yield break;
        isAttacking = true;
        animator.SetTrigger("Attack");
        cooldownTimer = attackInfo.Cooldown;

        yield return new WaitForSeconds(attackInfo.DelayAfterTrigger);

        if (IsCollidingWithPlayer())
        {
            playerStats.TakeDamage(attackInfo.Damage);
            SFXManager.instance.PlayEnemyAttackSFX(enemyData.Name, transform.position);
        }

        isAttacking = false;
    }

    private bool IsCollidingWithPlayer()
    {
        List<Collider2D> results = new();
        ContactFilter2D filter = new();
        filter.SetLayerMask(LayerMask.GetMask("Player"));
        filter.useLayerMask = true;
        
        Physics2D.OverlapCollider(attackArea, filter, results);

        return results.Contains(playerColl);
    }
}
