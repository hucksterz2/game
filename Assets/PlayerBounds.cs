using UnityEngine;

public class PlayerBounds : MonoBehaviour
{
    [Header("Границы карты")]
    public float minX = -50f;
    public float maxX =  50f;
    public float minY = -15f;

    [Header("Точка возрождения (оставь пустым = стартовая позиция)")]
    public Transform respawnPoint;

    private Vector3 spawnPos;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
        spawnPos = respawnPoint != null ? respawnPoint.position : transform.position;
    }

    void Update()
    {
        Vector3 p = transform.position;

        bool outOfBounds = p.x < minX || p.x > maxX || p.y < minY;

        if (outOfBounds)
        {
            transform.position = spawnPos;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}
