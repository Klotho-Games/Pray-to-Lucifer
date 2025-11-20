using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int MaxHealth { get; private set; } = 1000;
    public int CurrentHealth { get; private set; } = 1000;
    public int MaxSoul { get; private set; } = 5000;
    public int CurrentSoul { get; private set; } = 0;

    public event System.Action OnHealthChanged;
    public event System.Action OnSoulChanged;

    public void TakeDamage(int damage)
    {
        var temp = CurrentHealth - damage;
        if (temp < 0)
        {
            CurrentHealth = 0;
            Die();
        }
        else
            CurrentHealth = temp;

        OnHealthChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        var temp = CurrentHealth + amount;
        if (temp > MaxHealth)
            CurrentHealth = MaxHealth;
        else
            CurrentHealth = temp;
        
        OnHealthChanged?.Invoke();
    }

    public void ResetCurrentHealth()
    {
        CurrentHealth = MaxHealth;

        OnHealthChanged?.Invoke();
    }

    public void TakeSoul(int amount)
    {
        var temp = CurrentSoul - amount;
        if (temp < 0)
            CurrentSoul = 0;
        else
            CurrentSoul = temp;

        OnSoulChanged?.Invoke();
    }

    public void AddSoul(int amount)
    {
        var temp = CurrentSoul + amount;
        if (temp > MaxSoul)
            CurrentSoul = MaxSoul;
        else
            CurrentSoul = temp;

        OnSoulChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Implement on player death logic here
    }
}
