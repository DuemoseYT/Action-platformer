using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lives-based player health. Defaults to 3 lives; change maxLives (or call SetMaxLives at
/// runtime) to scale it. Any hit costs 1 life by default regardless of the "amount" a source
/// passes, since lives don't subdivide the way HP does — see TakeDamage if you want amount
/// to matter (e.g. a boss hit costing 2 lives).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable, IInvulnerable
{
    [System.Serializable] public class LivesChangedEvent : UnityEvent<int, int> { }   // (current, max)

    [Header("Lives")]
    [Tooltip("Change this in the inspector, or call SetMaxLives() at runtime, to scale it.")]
    public int maxLives = 3;
    public int CurrentLives { get; private set; }

    [Header("Hit Response")]
    public float invulnTime = 1f;
    public float knockbackForce = 10f;
    public SpriteRenderer spriteRenderer;
    public float flashInterval = 0.08f;   // blink rate during the invuln window

    [Header("References (optional)")]
    public CameraFollow2D cam;
    public PlayerSFX sfx;
    public float hitShake = 0.15f;

    [Header("Events")]
    public LivesChangedEvent onLivesChanged;   // hook UI here: (current, max) -> update hearts/icons
    public UnityEvent onDamaged;
    public UnityEvent onDeath;                 // hook game-over / respawn logic here

    public bool IsAlive => CurrentLives > 0;
    public bool IsInvulnerable => invulnTimer > 0f;

    private Rigidbody2D rb;
    private float invulnTimer;
    private Coroutine flashRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!cam && Camera.main) cam = Camera.main.GetComponent<CameraFollow2D>();
        if (!sfx) sfx = GetComponent<PlayerSFX>();

        CurrentLives = maxLives;
    }

    private void Update()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    // ── IDamageable ──────────────────────────────────────────────

    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 knockbackDir)
    {
        if (!IsAlive || IsInvulnerable) return;

        CurrentLives = Mathf.Max(0, CurrentLives - Mathf.Max(1, amount));
        invulnTimer = invulnTime;

        Vector2 push = new Vector2(knockbackDir.x, Mathf.Max(knockbackDir.y, 0f));
        if (push.sqrMagnitude < 0.01f) push = Vector2.up;
        // UNITY 6: swap `linearVelocity` for `velocity` if you're on an older Unity version.
        rb.linearVelocity = push.normalized * knockbackForce;

        if (cam) cam.Shake(hitShake);
        sfx?.PlayHurt();
        onDamaged?.Invoke();
        onLivesChanged?.Invoke(CurrentLives, maxLives);

        if (spriteRenderer)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (CurrentLives <= 0) Die();
    }

    private IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < invulnTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flashInterval);
            t += flashInterval;
        }
        spriteRenderer.enabled = true;
    }

    private void Die()
    {
        sfx?.PlayDeath();
        onDeath?.Invoke();
        // Project-specific from here: hook onDeath in the inspector to reload the scene,
        // show a game-over screen, respawn at a checkpoint, etc.
    }

    // ── public API ───────────────────────────────────────────────

    /// <summary>Restore lives, capped at maxLives. Omit amount for a full heal.</summary>
    public void Heal(int amount = -1)
    {
        if (amount < 0) amount = maxLives;
        CurrentLives = Mathf.Min(maxLives, CurrentLives + amount);
        onLivesChanged?.Invoke(CurrentLives, maxLives);
    }

    /// <summary>Scale the life total at runtime — a life-up pickup, difficulty setting, etc.</summary>
    public void SetMaxLives(int newMax, bool healToFull = true)
    {
        maxLives = Mathf.Max(1, newMax);
        CurrentLives = healToFull ? maxLives : Mathf.Min(CurrentLives, maxLives);
        onLivesChanged?.Invoke(CurrentLives, maxLives);
    }

    /// <summary>Full reset for a fresh run or a respawn.</summary>
    public void ResetHealth()
    {
        CurrentLives = maxLives;
        invulnTimer = 0f;
        if (spriteRenderer) spriteRenderer.enabled = true;
        onLivesChanged?.Invoke(CurrentLives, maxLives);
    }
}