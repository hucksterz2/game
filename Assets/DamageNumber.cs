using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class DamageNumber : MonoBehaviour
{
    public static void Show(int amount, Vector3 worldPos)
    {
        var go = new GameObject("DmgNum");
        go.transform.position = worldPos;
        go.AddComponent<DamageNumber>().Init(amount);
    }

    private TextMesh tm;
    private Color    baseColor;
    private float    timer;
    private float    baseScale;

    private const float DURATION   = 0.85f;
    private const float RISE_SPEED = 1.4f;

    void Init(int amount)
    {
        tm = GetComponent<TextMesh>();
        tm.text          = amount.ToString();
        tm.characterSize = 0.18f;
        tm.fontSize      = 36;
        tm.fontStyle     = FontStyle.Bold;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;

        baseColor = amount >= 20
            ? new Color(1.0f, 0.30f, 0.10f)
            : new Color(1.0f, 0.92f, 0.15f);
        tm.color = baseColor;

        baseScale = Mathf.Lerp(0.55f, 1.2f, Mathf.Clamp01(amount / 30f));
        transform.localScale = Vector3.one * baseScale;

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingLayerName = "Default";
        if (mr != null) mr.sortingOrder = 25;

        timer = DURATION;
    }

    void Update()
    {
        if (tm == null) { Destroy(gameObject); return; }

        timer -= Time.deltaTime;
        if (timer <= 0f) { Destroy(gameObject); return; }

        transform.position += Vector3.up * (RISE_SPEED * Time.deltaTime);

        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        float t     = timer / DURATION;
        float alpha = t < 0.4f ? t / 0.4f : 1f;
        tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        float sc = t > 0.8f ? Mathf.Lerp(1.0f, 1.3f, (t - 0.8f) / 0.2f) : 1.0f;
        transform.localScale = Vector3.one * (baseScale * sc);
    }
}
