using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Игрок умер!");
    }
}