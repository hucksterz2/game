using System.Collections;
using UnityEngine;

public class BossDefeatTrigger : MonoBehaviour
{
    [Header("HP")]
    public BossHealth bossHealth;

    [Header("Диалог")]
    public Sprite portrait;
    [TextArea(2, 5)] public string[] dialogueLines;

    [Header("Параметры")]
    public string dieAnimName = "death";
    public float deathAnimDuration = 1.5f;
    public bool pauseTimeDuringDialog = true;
    public bool destroyBossAfterDialog = true;

    [Header("Флаг")]
    public string setFlag = "bossDefeated";

    private bool triggered = false;

    void Start()
    {
        if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
        if (bossHealth == null) bossHealth = GetComponentInChildren<BossHealth>();
    }

    void Update()
    {
        if (triggered) return;
        if (bossHealth == null) return;

        if (bossHealth.currentHP <= 0)
        {
            triggered = true;
            StartCoroutine(DefeatRoutine());
        }
    }

    public void TriggerManually()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(DefeatRoutine());
    }

    IEnumerator DefeatRoutine()
    {
        Debug.Log("BossDefeat: смерть босса");

        BossHealthBarUI[] bars = FindObjectsByType<BossHealthBarUI>(FindObjectsSortMode.None);
        foreach (var bar in bars) Destroy(bar);

        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in allCanvases)
        {
            Transform hpPanel = canvas.transform.Find("BossHPPanel");
            if (hpPanel != null) Destroy(hpPanel.gameObject);
        }

        MonoBehaviour[] allScripts = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in allScripts)
        {
            if (s == this) continue;
            if (s is BossHealth) continue;
            s.CancelInvoke();
            s.StopAllCoroutines();
            s.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 1f;
            foreach (var p in anim.parameters)
            {
                if (p.name != dieAnimName) continue;
                if (p.type == AnimatorControllerParameterType.Trigger) anim.SetTrigger(dieAnimName);
                else if (p.type == AnimatorControllerParameterType.Bool) anim.SetBool(dieAnimName, true);
                break;
            }
        }

        yield return new WaitForSeconds(deathAnimDuration);

        if (anim != null) anim.speed = 0f;
        Debug.Log("BossDefeat: замер на последнем кадре");

        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }

        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            if (pauseTimeDuringDialog) Time.timeScale = 0f;

            DialogueManager.Instance.StartDialogue(portrait, dialogueLines);

            yield return null;
            while (DialogueManager.Instance.IsActive)
                yield return null;

            if (pauseTimeDuringDialog) Time.timeScale = 1f;
        }

        Debug.Log("BossDefeat: диалог закрыт");

        if (!string.IsNullOrEmpty(setFlag))
            GameFlags.Set(setFlag, true);

        if (destroyBossAfterDialog)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}