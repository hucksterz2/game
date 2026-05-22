using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerDefeatEnding : MonoBehaviour
{
    public static string LastBossLevel = "";

    [Header("Диалог поражения")]
    public Sprite portrait;
    [TextArea(2, 5)] public string[] dialogueLines;

    [Header("Концовка")]
    public string defeatSceneName = "DefeatScene";
    public float delayBeforeDialog = 1.2f;
    public float fadeDuration = 1.5f;

    private bool triggered = false;
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void Update()
    {
        if (triggered) return;

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
        }

        if (playerHealth.currentHP <= 0)
        {
            triggered = true;
            LastBossLevel = SceneManager.GetActiveScene().name;

            BossHPBar[] hpBars = FindObjectsByType<BossHPBar>(FindObjectsSortMode.None);
            foreach (var bar in hpBars) Destroy(bar.gameObject);

            GameObject handler = new GameObject("DefeatHandler");
            DefeatSequencer seq = handler.AddComponent<DefeatSequencer>();
            seq.Run(portrait, dialogueLines, defeatSceneName, fadeDuration, delayBeforeDialog, playerHealth);
        }
    }
}

public class DefeatSequencer : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private bool keepKillingDeathCanvas = true;

    public void Run(Sprite portrait, string[] lines, string scene, float fade, float delay, PlayerHealth ph)
    {
        playerHealth = ph;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Sequence(portrait, lines, scene, fade, delay));
        StartCoroutine(KillDeathCanvas());
    }

    IEnumerator KillDeathCanvas()
    {
        while (keepKillingDeathCanvas)
        {
            if (playerHealth != null) playerHealth.StopAllCoroutines();
            GameObject dc = GameObject.Find("DeathCanvas");
            if (dc != null) Destroy(dc);
            yield return null;
        }
    }

    IEnumerator Sequence(Sprite portrait, string[] lines, string sceneName, float fadeDuration, float delayBeforeDialog)
    {
        yield return new WaitForSecondsRealtime(delayBeforeDialog);
        if (playerHealth != null) playerHealth.StopAllCoroutines();
        Time.timeScale = 0f;

        if (lines != null && lines.Length > 0)
        {
            if (DialogueManager.Instance == null)
            {
                GameObject go = new GameObject("DialogueManager");
                go.AddComponent<DialogueManager>();
            }

            Time.timeScale = 0f;
            DialogueManager.Instance.StartDialogue(portrait, lines);
            yield return null;
            while (DialogueManager.Instance.IsActive)
            {
                if (Time.timeScale != 0f) Time.timeScale = 0f;
                yield return null;
            }
        }

        GameObject canvasGO = new GameObject("FadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject fadeGO = new GameObject("Fade");
        fadeGO.transform.SetParent(canvasGO.transform, false);
        Image fade = fadeGO.AddComponent<Image>();
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
        RectTransform fr = fadeGO.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            fade.color = new Color(0, 0, 0, a);
            yield return null;
        }

        keepKillingDeathCanvas = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}