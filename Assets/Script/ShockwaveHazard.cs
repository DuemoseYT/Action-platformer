using UnityEngine;

/// <summary>
/// A damaging wave that travels along the ground after a boss slam. Spawn it with Init(...)
/// at the exact moment the hand hits the floor; it moves in one direction, hurts the player
/// on contact (respecting their invulnerability, same as everything else), and destroys
/// itself after traveling maxDistance, living too long, or hitting a wall.
/// If no SpriteRenderer is present it builds a simple visible bar at runtime — no art asset
/// required to test with, though you're free to add your own sprite/animation instead.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ShockwaveHazard : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float knockbackForce = 12f;
    [Tooltip("Scales how much harder this launches the player upward vs a normal hit (1 = normal).")]
    public float launchMultiplier = 2.2f;

    [Header("Movement")]
    public float speed = 10f;
    public float maxDistance = 14f;
    public float maxLifetime = 3f;
    [Tooltip("Optional: destroy early if it hits something on this layer (a wall).")]
    public LayerMask wallLayer;

    [Header("Auto Visual (used only if no SpriteRenderer exists)")]
    public Vector2 visualSize = new Vector2(2f, 0.5f);
    public Color visualColor = new Color(1f, 0.45f, 0.1f, 0.9f);

    private static Sprite sharedSprite;

    private Vector2 direction = Vector2.right;
    private Vector3 startPos;
    private float lifeTimer;

    private void Awake()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        if (box.size == Vector2.one) box.size = visualSize;   // only override the default

        if (!GetComponent<SpriteRenderer>())
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSprite();
            sr.color = visualColor;
            sr.sortingOrder = 50;
            transform.localScale = new Vector3(visualSize.x, visualSize.y, 1f);
        }
    }

    /// <summary>Call right after Instantiate to set it moving.</summary>
    public void Init(Vector2 travelDirection, float travelSpeed, int dmg, float knockback, float distance)
    {
        direction = travelDirection.sqrMagnitude > 0.01f ? travelDirection.normalized : Vector2.right;
        speed = travelSpeed;
        damage = dmg;
        knockbackForce = knockback;
        maxDistance = distance;
        startPos = transform.position;

        if (direction.x < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer += Time.deltaTime;
        if (lifeTimer > maxLifetime || Vector2.Distance(startPos, transform.position) > maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player")) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null || !dmg.IsAlive) return;
        if (InvulnerabilityUtil.IsInvulnerable(other)) return;

        // mostly vertical launch — a ground slam should pop the player up, not just push them sideways
        Vector2 knockDir = new Vector2(direction.x * 0.3f, 1f) * launchMultiplier;
        dmg.TakeDamage(damage, transform.position, knockDir);
    }

    /// <summary>Soft horizontal bar, generated once and reused — no art asset needed.</summary>
    private static Sprite GetOrCreateSprite()
    {
        if (sharedSprite) return sharedSprite;

        const int w = 64, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float edgeFade = Mathf.Clamp01(Mathf.Min(x, w - 1 - x) / 10f);
            float vertFade = 1f - Mathf.Abs((y - h * 0.5f) / (h * 0.5f));
            pixels[y * w + x] = new Color(1f, 1f, 1f, edgeFade * vertFade);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        sharedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return sharedSprite;
    }
}