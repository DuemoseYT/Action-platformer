using System.Collections;
using UnityEngine;

/// <summary>
/// Simple patrolling enemy you can hit, kill and pogo off.
/// Walks back and forth, turns at walls and ledges, flashes and gets knocked back
/// when hit. Stays pogoable even while dying so you can bounce off the kill.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy2D : MonoBehaviour, IDamageable, IPogoable
{
    [Header("Health")]
    public int maxHealth = 3;
    public float invulnTime = 0.15f;
    [Tooltip("How long the corpse hangs around before being destroyed.")]
    public float deathDelay = 0.35f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public bool  patrol = true;
    public int   startDirection = 1;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackTime  = 0.15f;

    [Header("Pogo")]
    [Tooltip("Can the player bounce off this one?")]
    public bool pogoable = true;
    [Tooltip("Stays bouncy while dying so a killing blow still launches you.")]
    public bool pogoableWhileDying = true;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float playerKnockback = 14f;

    [Header("Checks")]
    public LayerMask groundLayer;
    [Tooltip("How far ahead (in the direction of travel) the wall check sits.")]
    public float wallCheckDistance = 0.5f;
    [Tooltip("How far ahead the edge/ledge check sits.")]
    public float edgeCheckDistance = 0.5f;
    [Tooltip("How far below the enemy's feet the edge check looks for ground.")]
    public float edgeCheckDepth = 0.4f;
    public Vector2 checkSize = new Vector2(0.12f, 0.12f);

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Color hitFlashColor = Color.white;
    public float hitFlashTime = 0.08f;
    [Tooltip("A random one of these is shown briefly whenever this enemy is hit — wince frames, damage poses, whatever variety you want. Leave empty to just use the color flash.")]
    public Sprite[] hitSprites;

    private Rigidbody2D rb;
    private int   dir;
    private int   health;
    private float invulnTimer, knockbackTimer;
    private bool  dying;
    private Color baseColor;
    private Coroutine hitRoutine;
    private Sprite currentHitSprite;
    private float hitSpriteTimer;

    public bool IsAlive => !dying && health > 0;
    public bool CanPogo => pogoable && (IsAlive || pogoableWhileDying);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer) baseColor = spriteRenderer.color;

        health = maxHealth;
        dir = startDirection >= 0 ? 1 : -1;
    }

    private void Update()
    {
        invulnTimer    -= Time.deltaTime;
        knockbackTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        // Runs after Animator evaluation, so forcing the sprite here guarantees it's what
        // actually shows on screen this frame — no need to find or disable any Animator.
        if (hitSpriteTimer > 0f && spriteRenderer)
        {
            spriteRenderer.sprite = currentHitSprite;
            hitSpriteTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (dying || knockbackTimer > 0f || !patrol) return;

        // turn around at a wall or a ledge — positions are derived fresh from `dir` every
        // tick, so there's no mutable state that can get out of sync and cause flip-flopping
        Vector2 origin = transform.position;
        Vector2 wallCheckPos = origin + new Vector2(dir * wallCheckDistance, 0f);
        Vector2 edgeCheckPos = origin + new Vector2(dir * edgeCheckDistance, -edgeCheckDepth);

        bool wallAhead   = Physics2D.OverlapBox(wallCheckPos, checkSize, 0f, groundLayer);
        bool groundAhead = Physics2D.OverlapBox(edgeCheckPos, checkSize, 0f, groundLayer);
        if (wallAhead || !groundAhead) Flip();

        // UNITY 6: swap `velocity` for `linearVelocity` in this method.
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        dir *= -1;
        if (spriteRenderer) spriteRenderer.flipX = dir < 0;
    }

    // ── IDamageable ──────────────────────────────────────────────

    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 knockbackDir)
    {
        if (!IsAlive || invulnTimer > 0f) return;

        health -= amount;
        invulnTimer = invulnTime;

        // pushed away from the player, never straight down into the floor
        Vector2 push = new Vector2(knockbackDir.x, Mathf.Max(knockbackDir.y, 0f)).normalized;
        if (push.sqrMagnitude < 0.01f) push = Vector2.up;
        rb.linearVelocity = push * knockbackForce;
        knockbackTimer = knockbackTime;

        if (spriteRenderer)
        {
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(FlashColor());
        }

        if (hitSprites != null && hitSprites.Length > 0)
        {
            currentHitSprite = hitSprites[Random.Range(0, hitSprites.Length)];
            hitSpriteTimer = hitFlashTime;
        }

        if (health <= 0) StartCoroutine(Die());
    }

    private IEnumerator FlashColor()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        if (spriteRenderer) spriteRenderer.color = baseColor;
        hitRoutine = null;
    }

    private IEnumerator Die()
    {
        dying = true;
        // TODO: play a death animation / particle / sound here
        if (spriteRenderer) spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);

        // pogo window stays open briefly after the killing blow
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    // ── contact damage ───────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D col)  => TouchPlayer(col.collider);
    private void OnCollisionStay2D(Collision2D col)   => TouchPlayer(col.collider);
    private void OnTriggerStay2D(Collider2D other)    => TouchPlayer(other);

    private void TouchPlayer(Collider2D other)
    {
        if (dying || contactDamage <= 0) return;
        if (!other.CompareTag("Player")) return;
        // just got hit (e.g. the down-attack that's pogoing off us) — don't fight that knockback
        if (invulnTimer > 0f) return;
        // player has a temporary immunity window (mid-slide, mid-dash, etc.)
        if (InvulnerabilityUtil.IsInvulnerable(other)) return;

        var playerDmg = other.GetComponentInParent<IDamageable>();
        if (playerDmg == null || !playerDmg.IsAlive) return;

        Vector2 away = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        playerDmg.TakeDamage(contactDamage, transform.position, away);

        var prb = other.attachedRigidbody;
        if (prb) prb.linearVelocity = new Vector2(away.x, 0.6f).normalized * playerKnockback;
    }

    private void OnDrawGizmosSelected()
    {
        int previewDir = Application.isPlaying ? dir : (startDirection >= 0 ? 1 : -1);
        Vector2 origin = transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(origin + new Vector2(previewDir * wallCheckDistance, 0f), checkSize);
        Gizmos.DrawWireCube(origin + new Vector2(previewDir * edgeCheckDistance, -edgeCheckDepth), checkSize);
    }
}