using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Scriptable Objects/Enemy")]
public class EnemySO : ScriptableObject
{
    public enum AttackType { Melee, Projectile, Grenade }
    
    [System.Serializable]
    public class Attack
    {
        public AttackType attackType = AttackType.Melee;
        public int attackDamage = 20;
        
        [Tooltip("How close the enemy needs to be to attack")]
        public float attackRange = 10f;
        
        [Tooltip("Time between attacks")]
        public float attackCooldown = 1.5f;
    }
    
    [Header("Identity")]

    public string enemyName = "New Enemy";
    public bool isLava = false;


    [Header("Stats")]
    public int maxHP = 100;
    public float speed = 5f;
    
    /// <summary>
    /// How much health is regenerated per second
    /// </summary>
    [Tooltip("How much health is regenerated per second")]
    public float regeneration = 0f;
    
    public int soulRewardAmount = 10;


    [Header("Behavior")]
    
    [SetValue("isPlacer", false, false)]
    public bool isEnergy = false;


    [Header("Attack")]
    public List<Attack> attacks = new();
}
