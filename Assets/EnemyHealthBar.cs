using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public int maxHP = 60;
    public int currentHP;
    public Vector3 barOffset = new Vector3(0, 1.2f, 0);

    private GameObject barObj;
    private Image fillImage;
    private Canvas worldCanvas;

    void Start()
    {
        currentHP = maxHP;
        CreateBar();
    }

    void CreateBar()
    {
        barObj = new GameObject("EnemyHPBar");
        worldCanvas = barObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = barObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120f, 15f);
        barObj.transform.localScale = Vector3.one * 0.01f;

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(barObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barObj.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.9f, 0.15f, 0.15f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (barObj == null) return;

        barObj.transform.position = transform.position + barOffset;
        barObj.transform.rotation = Camera.main.transform.rotation;

        float ratio = (float)currentHP / maxHP;
        fillImage.rectTransform.anchorMax = new Vector2(ratio, 1f);

        fillImage.color = Color.Lerp(
            new Color(0.9f, 0.1f, 0.1f),
            new Color(0.1f, 0.8f, 0.2f),
            ratio);

        barObj.SetActive(currentHP < maxHP);
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (barObj != null) barObj.SetActive(true);
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        Destroy(barObj);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (barObj != null) Destroy(barObj);
    }
}
