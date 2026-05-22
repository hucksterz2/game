using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FinalBossEnding : MonoBehaviour
{
    [Header("Босс (фаза 2 / финальный)")]
    public HeavyBanditBoss bossToWatch;

    [Header("Диалог после смерти")]
    public Sprite portrait;
    [TextArea(2, 5)] public string[] dialogueLines;

    [Header("Концовка")]
    public string endingSceneName = "End";
    public float delayBeforeDialog = 1.8f;
    public float fadeDuration = 1.5f;
    public bool pauseDuringDialog = true;

    private bool triggered = false;

    void Start()
    {
        if (bossToWatch == null) bossToWatch = GetComponent<HeavyBanditBoss>();
    }

    void Update()
    {
        if (triggered) return;
        if (bossToWatch == null) return;

        if (bossToWatch.IsDead)
        {
            triggered = true;
            GameObject handler = new GameObject("EndingHandler");
            EndingSequencer seq = handler.AddComponent<EndingSequencer>();
            seq.Run(portrait, dialogueLines, endingSceneName, fadeDuration, delayBeforeDialog, pauseDuringDialog);
        }
    }
}

public class EndingSequencer : MonoBehaviour
{
    public void Run(Sprite portrait, string[] lines, string scene, float fade, float delay, bool pause)
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Sequence(portrait, lines, scene, fade, delay, pause));
    }

    IEnumerator Sequence(Sprite portrait, string[] lines, string sceneName, float fadeDuration, float delayBeforeDialog, bool pauseDuringDialog)
    {
        yield return new WaitForSeconds(delayBeforeDialog);

        if (lines != null && lines.Length > 0)
        {
            if (DialogueManager.Instance == null)
            {
                GameObject go = new GameObject("DialogueManager");
                go.AddComponent<DialogueManager>();
            }

            if (pauseDuringDialog) Time.timeScale = 0f;
            DialogueManager.Instance.StartDialogue(portrait, lines);
            yield return null;
            while (DialogueManager.Instance.IsActive) yield return null;
            if (pauseDuringDialog) Time.timeScale = 1f;
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

        Time.timeScale = 1f;

        if (CoinManager.Instance != null) CoinManager.CommitLevelCoins();

        SceneManager.LoadScene(sceneName);
    }
}