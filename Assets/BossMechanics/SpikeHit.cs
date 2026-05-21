using UnityEngine;

public class SpikeHit : MonoBehaviour
{
    private PlayerHealth playerHealth;
    public int damage = 15;

    public void Init(PlayerHealth ph)
    {
        playerHealth = ph;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}