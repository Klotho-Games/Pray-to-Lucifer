using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Scriptable Objects/Enemy")]
public class Enemy : ScriptableObject
{
    [Header("Identity")]

    public string enemyName = "New Enemy";
    public bool isLava = false;


    [Header("Stats")]
    public float size = 1f;
    public int maxHP = 100;
    public float speed = 5f;
    
    /// <summary>
    /// Damage reduction (division) applied to incoming damage
    /// </summary>
    [Tooltip("Damage reduction (division) applied to incoming damage")]
    public int armor = 1;
    
    /// <summary>
    /// How much health is regenerated per second
    /// </summary>
    [Tooltip("How much health is regenerated per second")]
    public float regeneration = 5f;
    
    public float rewardAmount = 10f;


    [Header("Behavior")]
    
    [SetValue("isPlacer", false, false)]
    public bool isPassthrough = false;

    [ShowIf(true, "isPassthrough", "attackType", AttackType.Melee)]
    public bool isPlacer = false;


    [Header("Attack")]
    [SetValue("isPlacer", false, AttackType.Ranged)]
    public AttackType attackType = AttackType.Melee;
    public float attackDamage = 20f;
    
    /// <summary>
    /// How close the enemy needs to be to attack (for ranged attacks)
    /// </summary>
    [Tooltip("How close the enemy needs to be to attack (for ranged attacks)")]
    [ShowIf("attackType", AttackType.Ranged)]
    public float attackRange = 10f;
    
    /// <summary>
    /// Frequency of attacks (for ranged attacks)
    /// </summary>
    [Tooltip("Frequency of attacks (for ranged attacks)")]
    [ShowIf("attackType", AttackType.Ranged)]
    public float attackCooldown = 1.5f;

    public enum AttackType { Melee, Ranged }
}
