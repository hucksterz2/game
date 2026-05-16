using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BombThrower : MonoBehaviour
{
    [Header("Бомба (G)")]
    public float bombCooldown = 3f;
    public float throwForceX  = 11f;
    public float throwForceY  = 5f;
    public int   bombDamage   = 25;
    public float bombRadius   = 2f;
    public float bombFuseTime = 2f;

    private float  bombTimer = 0f;
    private Image  cdFill;
    private Text   cdLabel;

    void Start() => BuildUI();

    void Update()
    {
        bombTimer -= Time.deltaTime;
        RefreshUI();

        if (Keyboard.current.gKey.wasPressedThisFrame && bombTimer <= 0f)
            ThrowBomb();
    }

    void ThrowBomb()
    {
        bombTimer = bombCooldown;

        float dir = transform.localScale.x < 0 ? 1f : -1f;

        var bomb = new GameObject("Bomb");
        bomb.transform.position = (Vector2)transform.position + new Vector2(dir * 0.6f, 0.3f);

        var rb2d = bomb.AddComponent<Rigidbody2D>();
        rb2d.gravityScale   = 1.4f;
        rb2d.linearVelocity = new Vector2(dir * throwForceX, throwForceY);

        var col = bomb.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;

        var sr = bomb.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite(new Color(0.12f, 0.12f, 0.12f));
        sr.sortingOrder = 5;
        bomb.transform.localScale = Vector3.one * 0.35f;

        var playerCol = GetComponent<Collider2D>() ?? GetComponentInParent<Collider2D>();
        if (playerCol != null) Physics2D.IgnoreCollision(col, playerCol);

        var bs         = bomb.AddComponent<BombScript>();
        bs.fuseTime    = bombFuseTime;
        bs.damage      = bombDamage;
        bs.blastRadius = bombRadius;
    }

    void BuildUI()
    {
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) return;

        var panel = new GameObject("BombCooldownUI");
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(12f, 52f);
        rt.sizeDelta = new Vector2(58f, 58f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.06f, 0.9f);

        var icon = new GameObject("BombIcon");
        icon.transform.SetParent(panel.transform, false);
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = new Color(0.2f, 0.2f, 0.2f);
        var ir = icon.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.22f, 0.22f); ir.anchorMax = new Vector2(0.78f, 0.78f);
        ir.sizeDelta = Vector2.zero;

        var fillGO = new GameObject("CDFill");
        fillGO.transform.SetParent(panel.transform, false);
        cdFill              = fillGO.AddComponent<Image>();
        cdFill.color        = new Color(1f, 0.45f, 0.1f, 0.68f);
        cdFill.type         = Image.Type.Filled;
        cdFill.fillMethod   = Image.FillMethod.Radial360;
        cdFill.fillOrigin   = (int)Image.Origin360.Top;
        cdFill.fillClockwise = false;
        cdFill.fillAmount   = 0f;
        var fr = fillGO.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.sizeDelta = Vector2.zero;

        var textGO = new GameObject("CDLabel");
        textGO.transform.SetParent(panel.transform, false);
        cdLabel           = textGO.AddComponent<Text>();
        cdLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cdLabel.fontSize  = 15;
        cdLabel.color     = new Color(0.9f, 0.9f, 0.9f);
        cdLabel.fontStyle = FontStyle.Bold;
        cdLabel.alignment = TextAnchor.MiddleCenter;
        cdLabel.text      = "G";
        var tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
    }

    void RefreshUI()
    {
        if (cdFill  != null) cdFill.fillAmount = Mathf.Clamp01(bombTimer / bombCooldown);
        if (cdLabel != null) cdLabel.text = bombTimer > 0.05f
            ? Mathf.CeilToInt(bombTimer).ToString()
            : "G";
    }

    static Sprite MakeSquareSprite(Color col)
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
