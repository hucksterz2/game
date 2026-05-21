using UnityEngine;
using System.Collections;

public class BringerOfDeathBoss : MonoBehaviour
{
    [Header("Параметры")]
    public int   maxHP         = 500;
    public float speed         = 1.8f;
    public int   meleeDamage   = 40;
    public int   castDamage    = 35;
    public float meleeRange    = 2.5f;
    public float castRange     = 8f;
    public float attackCD      = 2.2f;

    [Header("Имя в HP-баре")]
    public string bossName     = "Bringer Of Death";

    int   currentHP;
    float attackTimer;
    float baseScaleX, baseScaleY, baseScaleZ;

    Rigidbody2D    rb;
    Animator       anim;
    Transform      player;
    PlayerHealth   playerHealth;
    BossHPBar      hpBar;
    SpriteRenderer sr;

    enum State { Idle, Chase, Attack, Cast, Hurt, Dead }
    State state = State.Idle;

    void Start()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);
        sr   = GetComponentInChildren<SpriteRenderer>();

        currentHP  = maxHP;
        baseScaleX = Mathf.Abs(transform.localScale.x);
        baseScaleY = transform.localScale.y;
        baseScaleZ = transform.localScale.z;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player       = pc.transform;
            playerHealth = pc.GetComponent<PlayerHealth>()
                        ?? pc.GetComponentInParent<PlayerHealth>()
                        ?? FindFirstObjectByType<PlayerHealth>();
        }

        hpBar = FindFirstObjectByType<BossHPBar>();
        if (hpBar != null)
        {
            hpBar.bossName = bossName;
            hpBar.SetVisible(true);
            hpBar.UpdateHP(1f);
        }

        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(0.5f);
        while (state != State.Dead)
        {
            if (player == null) { yield return null; continue; }

            float hdist = Mathf.Abs(transform.position.x - player.position.x);

            if (attackTimer > 0f) attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f && hdist <= meleeRange)
            {
                yield return StartCoroutine(DoMelee());
                attackTimer = attackCD;
            }
            else if (attackTimer <= 0f && hdist <= castRange)
            {
                yield return StartCoroutine(DoCast());
                attackTimer = attackCD;
            }
            else
            {
                yield return StartCoroutine(DoChase(0.5f));
            }
            yield return null;
        }
    }

    IEnumerator DoChase(float dur)
    {
        state = State.Chase;
        TriggerAnim("Walk");
        for (float t = 0f; t < dur && state != State.Dead; t += Time.deltaTime)
        {
            if (player != null)
            {
                int d = player.position.x > transform.position.x ? 1 : -1;
                rb.linearVelocity = new Vector2(speed * d, rb.linearVelocity.y);
                FacePlayer();
            }
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    IEnumerator DoMelee()
    {
        state = State.Attack;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FacePlayer();
        TriggerAnim("Attack");
        yield return new WaitForSeconds(0.45f);

        if (state == State.Dead) yield break;
        if (player != null && playerHealth != null
            && Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
        {
            float kbDir = player.position.x > transform.position.x ? 1f : -1f;
            var pRb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInParent<Rigidbody2D>();
            if (pRb != null) { pRb.linearVelocity = Vector2.zero; pRb.AddForce(new Vector2(kbDir * 10f, 4f), ForceMode2D.Impulse); }
            playerHealth.TakeDamage(meleeDamage);
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator DoCast()
    {
        state = State.Cast;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FacePlayer();
        TriggerAnim("Cast");
        yield return new WaitForSeconds(0.8f);

        if (state == State.Dead) yield break;
        if (player != null && playerHealth != null
            && Vector2.Distance(transform.position, player.position) <= castRange)
        {
            playerHealth.TakeDamage(castDamage);
        }
        yield return new WaitForSeconds(0.4f);
    }

    public void TakeDamage(int amount)
    {
        if (state == State.Dead) return;
        currentHP = Mathf.Max(0, currentHP - amount);

        BloodParticles.Spawn(transform.position + Vector3.up * 0.8f, amount, transform);
        DamageNumber.Show(amount, transform.position + Vector3.up * 1.6f);
        if (hpBar != null) hpBar.UpdateHP((float)currentHP / maxHP);

        if (currentHP <= 0)
        {
            state = State.Dead;
            StopAllCoroutines();
            StartCoroutine(DieRoutine());
            return;
        }
        TriggerAnim("Hurt");
    }

    IEnumerator DieRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;
        TriggerAnim("Death");
        if (hpBar != null) hpBar.SetVisible(false);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    void FacePlayer()
    {
        if (player == null) return;
        float d = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(-d * baseScaleX, baseScaleY, baseScaleZ);
    }

    void TriggerAnim(string name)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.name != name) continue;
            if (p.type == AnimatorControllerParameterType.Trigger) anim.SetTrigger(name);
            return;
        }
    }

    public bool IsDead => state == State.Dead;
}
