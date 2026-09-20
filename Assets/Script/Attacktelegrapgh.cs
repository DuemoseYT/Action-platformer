using UnityEngine;

/// <summary>
/// A "something is about to happen here" warning ring — pulses, grows slightly over its
/// lifetime, then destroys itself. Builds its own sprite at runtime, same approach as
/// HitSpark2D, so no art asset is required to start using it.
/// </summary>
public class AttackTelegraph : MonoBehaviour
{
    public Color color = new Color(1f, 0.15f, 0.15f, 0.85f);
    public float duration = 0.5f;
    public float startScale = 0.6f;
    public float endScale = 1.3f;
    public float pulseSpeed = 12f;

    private static Sprite sharedRing;
    private SpriteRenderer sr;
    private float timer;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateRing();
        sr.color = color;
        sr.sortingOrder = 60;
        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float p = Mathf.Clamp01(timer / duration);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);

        float pulse = 0.5f + 0.5f * Mathf.Sin(timer * pulseSpeed);
        var c = color;
        c.a = Mathf.Lerp(0.3f, color.a, pulse) * (1f - p * 0.3f);
        sr.color = c;

        if (timer >= duration) Destroy(gameObject);
    }

    /// <summary>Show a warning ring at a world position for a set duration.</summary>
    public static void Show(Vector3 position, float duration, Color color)
    {
        var go = new GameObject("AttackTelegraph");
        go.transform.position = position;
        var t = go.AddComponent<AttackTelegraph>();   // Awake runs now, using default duration/color
        t.duration = duration;
        t.color = color;
        // Update() hasn't run yet this frame, so these values take effect immediately.
    }

    private static Sprite GetOrCreateRing()
    {
        if (sharedRing) return sharedRing;

        const int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color[res * res];
        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float outerR = res * 0.5f - 2f;
        float innerR = outerR * 0.65f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            float a = 0f;
            if (d < outerR && d > innerR)
                a = Mathf.Clamp01(Mathf.Min(outerR - d, d - innerR) / 4f);
            pixels[y * res + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        sharedRing = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 32f);
        return sharedRing;
    }
}