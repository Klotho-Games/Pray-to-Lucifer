using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Scriptable Objects/Enemy")]
public class EnemySO : ScriptableObject
{
    public enum AttackType { Melee, Projectile, Grenade }
    
    [System.Serializable]
    public class Attack
    {
        public AttackType Type = AttackType.Melee;
        
        public int Damage = 20;
        
        [Tooltip("Time between attacks")]
        public float Cooldown = 1.5f;

        [Tooltip("The delay between attack geting triggered to dealing damage.\n\nSynchronize with animation")]
        public float DelayAfterTrigger = 0.15f;
    }
    
    [Header("Identity")]
    public bool IsLava = false;


    [Header("Stats")]
    public int MaxHP = 100;
    public float Speed = 5f;
    
    /// <summary>
    /// How much health is regenerated per second
    /// </summary>
    [Tooltip("How much health is regenerated per second")]
    public float Regeneration = 0f;
    
    public int SoulRewardAmount = 10;


    [Header("Behavior")]
    
    [SetValue("isPlacer", false, false)]
    public bool IsEnergy = false;


    [Header("Attack")]
    public List<Attack> Attacks = new();
}
