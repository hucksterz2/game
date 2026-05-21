using UnityEngine;

public class KnifeProjectile : MonoBehaviour
{
    public int   damage   = 8;
    public float lifetime = 4f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.isTrigger) return;
        PlayerHealth ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
        if (ph != null) { ph.TakeDamage(damage); Destroy(gameObject); return; }
        if (col.GetComponentInParent<HeavyBanditBoss>() == null) Destroy(gameObject);
    }
}
