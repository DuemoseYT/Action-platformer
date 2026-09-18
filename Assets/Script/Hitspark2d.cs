using UnityEngine;

/// <summary>
/// A one-shot particle burst for hit impacts. Builds its own ParticleSystem and material
/// at runtime — no prefab, sprite, or scene setup required. Just call HitSpark2D.Spawn(...)
/// from wherever damage lands and it does the rest, then destroys itself.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class HitSpark2D : MonoBehaviour
{
    [Header("Look")]
    public Color colorA = new Color(1f, 0.95f, 0.4f);   // bright core
    public Color colorB = new Color(1f, 0.35f, 0.1f);   // fading edge
    public int   burstCount = 14;
    public float minSpeed = 3f;
    public float maxSpeed = 8f;
    public float lifetime = 0.28f;
    public float startSize = 0.14f;
    public float gravityScale = 1.4f;
    [Tooltip("Sorting layer the particles render on, so they show up above your sprites.")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;

    private static Material sharedMaterial;
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        Build();
        ps.Play();
        Destroy(gameObject, lifetime + 0.4f);
    }

    private void Build()
    {
        var main = ps.main;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = startSize;
        main.startColor = colorA;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravityScale;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.01f;
        shape.arc = 360f;

        // fade from bright core color to edge color, then to transparent
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(colorA, 0f), new GradientColorKey(colorB, 0.6f), new GradientColorKey(colorB, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSharedMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
    }

    /// <summary>Soft round dot generated once and reused by every spark — no art asset needed.</summary>
    private static Material GetSharedMaterial()
    {
        if (sharedMaterial) return sharedMaterial;

        const int res = 32;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color[res * res];
        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (res * 0.5f);
            float a = Mathf.Clamp01(1f - d);
            a *= a;   // softer falloff toward the edge
            pixels[y * res + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        sharedMaterial.mainTexture = tex;
        return sharedMaterial;
    }

    /// <summary>
    /// Spawn a hit spark at a world position. Call this straight from wherever a hit lands —
    /// no prefab reference needed anywhere in your project.
    /// </summary>
    public static void Spawn(Vector3 position, Color? colorA = null, Color? colorB = null,
                              int burstCount = 14, float size = 0.14f)
    {
        var go = new GameObject("HitSpark");
        go.transform.position = position;

        var spark = go.AddComponent<HitSpark2D>();   // Awake runs now
        if (colorA.HasValue) spark.colorA = colorA.Value;
        if (colorB.HasValue) spark.colorB = colorB.Value;
        spark.burstCount = burstCount;
        spark.startSize  = size;
        // Start() runs later this frame, after these fields are set — that's when Build() fires.
    }
}