using UnityEngine;
using UnityEngine.InputSystem;

public class BossDamageTester : MonoBehaviour
{
    [Header("Test Settings")]
    public Key damageKey = Key.H;
    public int damagePerHit = 10;

    private BossHealth bossHealth;

    void Start()
    {
        bossHealth = GetComponent<BossHealth>();
        if (bossHealth == null)
            Debug.LogError("BossDamageTester: BossHealth не найден!");
    }

    void Update()
    {
        if (Keyboard.current[damageKey].wasPressedThisFrame)
        {
            if (bossHealth != null)
                bossHealth.TakeDamage(damagePerHit);
        }
    }
}