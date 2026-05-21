using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Coin : MonoBehaviour
{
    [Header("Сколько монет даёт")]
    public int value = 1;

    [Header("Радиус подбора")]
    public float pickupRadius = 0.8f;

    [Header("Покачивание")]
    public bool bobAnimation = true;
    public float bobSpeed = 2f;
    public float bobAmount = 0.15f;

    private Vector3 startPos;
    private Transform player;

    void Start()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = pickupRadius;
        startPos = transform.position;

        if (CoinManager.Instance == null)
        {
            GameObject go = new GameObject("CoinManager");
            go.AddComponent<CoinManager>();
        }
    }

    void Update()
    {
        if (bobAnimation)
            transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmount;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            CoinManager.Add(value);
            Destroy(gameObject);
        }
    }
}