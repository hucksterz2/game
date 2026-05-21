using UnityEngine;

public class BombScript : MonoBehaviour
{
    public float fuseTime    = 2f;
    public int   damage      = 25;
    public float blastRadius = 2f;

    private float          timer;
    private SpriteRenderer sr;
    private bool           exploded;

    private float pulsePhase;

    void Start()
    {
        timer = fuseTime;
        sr    = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (exploded) return;

        timer -= Time.deltaTime;

        if (sr != null)
        {
            float t = 1f - Mathf.Clamp01(timer / fuseTime);

            pulsePhase += Time.deltaTime * Mathf.Lerp(2f, 12f, t);
            float pulse = (Mathf.Sin(pulsePhase) * 0.5f + 0.5f);

            Color baseCol = Color.Lerp(new Color(0.15f, 0.10f, 0.10f), new Color(0.85f, 0.08f, 0.08f), t);
            sr.color = Color.Lerp(baseCol, Color.white, pulse * t * 0.45f);
        }

        if (timer <= 0f) Explode();
    }

    void Explode()
    {
        exploded = true;

        SpawnFlash();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
        System.Collections.Generic.HashSet<HeavyBanditBoss> hitBosses = new System.Collections.Generic.HashSet<HeavyBanditBoss>();
        foreach (var col in hits)
        {
            HeavyBanditBoss boss = col.GetComponent<HeavyBanditBoss>()
                                ?? col.GetComponentInParent<HeavyBanditBoss>();
            if (boss != null && !hitBosses.Contains(boss))
            {
                hitBosses.Add(boss);
                boss.TakeDamage(damage);
                continue;
            }

            BanditAI bandit = col.GetComponent<BanditAI>()
                           ?? col.GetComponentInParent<BanditAI>();
            if (bandit != null)
            {
                bandit.TakeDamage(damage);

                Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position);
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                dir = dir.normalized;
                Vector2 kbVel = new Vector2(dir.x * 9f, Mathf.Max(dir.y, 0.3f) * 9f);
                bandit.StunWithKnockback(1.2f, kbVel);
                continue;
            }

            PlayerHealth ph = col.GetComponent<PlayerHealth>()
                           ?? col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(Mathf.RoundToInt(damage * 0.5f));
            }
        }

        Destroy(gameObject);
    }

    void SpawnFlash()
    {
        var flash = new GameObject("BombFlash");
        flash.transform.position = transform.position;

        var sr2 = flash.AddComponent<SpriteRenderer>();
        sr2.sprite       = MakeCircleSprite(new Color(1f, 0.55f, 0.1f, 0.85f));
        sr2.sortingOrder = 10;
        flash.transform.localScale = Vector3.one * blastRadius * 2.2f;

        Destroy(flash, 0.18f);

        var core = new GameObject("BombCore");
        core.transform.position = transform.position;
        var sr3 = core.AddComponent<SpriteRenderer>();
        sr3.sprite       = MakeCircleSprite(new Color(1f, 1f, 0.85f, 0.95f));
        sr3.sortingOrder = 11;
        core.transform.localScale = Vector3.one * blastRadius * 0.7f;
        Destroy(core, 0.10f);
    }

    static Sprite MakeCircleSprite(Color col)
    {
        int size = 32;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false)
                   { filterMode = FilterMode.Bilinear };
        float r  = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01((r - dist));
            tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
