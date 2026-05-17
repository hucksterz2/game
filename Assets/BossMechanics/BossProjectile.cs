using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 16f;
    public int damage = 15;
    public float lifetime = 4f;

    private Vector2 direction;
    private PlayerHealth playerHealth;
    private CircleCollider2D col;
    private float enableTimer = 0.15f;
    private bool colEnabled = false;

    public void Init(Vector2 dir, PlayerHealth target, CircleCollider2D collider)
    {
        direction = dir.normalized;
        playerHealth = target;
        col = collider;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!colEnabled)
        {
            enableTimer -= Time.deltaTime;
            if (enableTimer <= 0f)
            {
                col.enabled = true;
                colEnabled = true;
            }
        }

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.CompareTag("Ground"))
            Destroy(gameObject);
    }
}