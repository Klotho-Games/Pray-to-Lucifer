using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int MaxHealth = 1000;
    public int CurrentHealth = 1000;
    public int MaxSoul = 500;
    public int CurrentSoul = 0;

    void Update()
    {
        if (CurrentSoul < 0)
            CurrentSoul = 0;
    }

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
    }

    public void AddSoul(int amount)
    {
        var temp = CurrentSoul + amount;
        if (temp > MaxSoul)
            CurrentSoul = MaxSoul;
        else
            CurrentSoul = temp;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Implement on player death logic here
    }
}
