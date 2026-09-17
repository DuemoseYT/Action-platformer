using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Directional melee attack with a Hollow Knight style pogo.
/// Left mouse button swings. Hold W to hit up, S to hit down, otherwise you swing
/// in the direction you're facing. A down attack that connects while airborne
/// bounces you back up, refills your dash and lets you chain off enemies forever.
/// </summary>
public class PlayerAttack2D : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement2D movement;
    public Rigidbody2D rb;
    public CameraFollow2D cam;          // optional, for shake

    [Header("Input")]
    public int attackMouseButton = 0;  // 0 = left
    public KeyCode keyUp = KeyCode.W;
    public KeyCode keyDown = KeyCode.S;

    [Header("Attack")]
    public int damage = 1;
    public float cooldown = 0.35f;

    [Tooltip("How long the hitbox stays live. Short = precise, long = forgiving.")]
    public float activeTime = 0.09f;

    public float knockbackForce = 12f;
    public LayerMask hittableLayers;

    [Tooltip("Surfaces you can pogo without any script on them, e.g. spikes.")]
    public LayerMask pogoSurfaceLayers;

    [Header("Hitbox Sizes & Offsets")]
    public Vector2 sideSize = new Vector2(1.6f, 1.1f);
    public float sideOffset = 1.0f;

    public Vector2 upSize = new Vector2(1.1f, 1.6f);
    public float upOffset = 1.0f;

    public Vector2 downSize = new Vector2(1.1f, 1.5f);
    public float downOffset = 1.0f;

    [Header("Pogo")]
    [Tooltip("Upward speed you get on a successful down-hit.")]
    public float pogoBounce = 17f;

    [Tooltip("Extra horizontal push in the direction you're already moving.")]
    public float pogoMomentum = 3.5f;

    [Tooltip("Cap on the horizontal speed pogo momentum can build to.")]
    public float pogoMaxHorizontal = 22f;

    public bool pogoRefillsDash = true;

    [Tooltip("Shorter cooldown right after a pogo so you can chain bounces.")]
    public float pogoCooldownOverride = 0.16f;

    [Header("Hit Spark")]
    public Color hitSparkColorA = new Color(1f, 0.95f, 0.4f);
    public Color hitSparkColorB = new Color(1f, 0.35f, 0.1f);

    [Header("Hit SFX")]
    [Tooltip("AudioSource used to play hit sounds.")]
    public AudioSource hitAudioSource;

    [Tooltip("Sounds played when an enemy takes damage.")]
    public AudioClip[] enemyHitSFX;

    [Tooltip("Sounds played when performing a successful pogo.")]
    public AudioClip[] pogoSFX;

    [Range(0f, 1f)]
    public float hitSFXVolume = 1f;

    [Tooltip("Randomly chooses a sound from the assigned array.")]
    public bool randomizeHitSFX = true;

    [Header("Juice")]
    [Tooltip("Freeze frames on hit, in seconds of real time. 0 to disable.")]
    public float hitStop = 0.05f;

    public float pogoShake = 0.12f;
    public float hitShake = 0.06f;

    [Header("Debug")]
    public bool drawHitboxGizmo = true;

    private float cooldownTimer;
    private bool isAttacking;

    private Vector2 lastHitboxCenter;
    private Vector2 lastHitboxSize;

    private readonly List<Collider2D> results = new List<Collider2D>();

    private enum AttackDir
    {
        Side,
        Up,
        Down
    }

    private AttackDir lastDir = AttackDir.Side;

    private void Awake()
    {
        if (!movement)
            movement = GetComponent<PlayerMovement2D>();

        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        if (!cam && Camera.main)
            cam = Camera.main.GetComponent<CameraFollow2D>();

        // Automatically find an AudioSource on the player if one wasn't assigned.
        if (!hitAudioSource)
            hitAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(attackMouseButton) &&
            cooldownTimer <= 0f &&
            !isAttacking)
        {
            StartCoroutine(DoAttack());
        }
    }

    private IEnumerator DoAttack()
    {
        isAttacking = true;
        cooldownTimer = cooldown;

        AttackDir dir = AttackDir.Side;

        if (Input.GetKey(keyUp))
            dir = AttackDir.Up;
        else if (Input.GetKey(keyDown))
            dir = AttackDir.Down;

        lastDir = dir;

        // TODO: trigger your swing animation here.
        // Example:
        // animator.SetTrigger("Attack");

        var alreadyHit = new HashSet<Collider2D>();

        float t = 0f;
        bool pogoed = false;

        while (t < activeTime)
        {
            GetHitbox(dir, out Vector2 center, out Vector2 size);

            lastHitboxCenter = center;
            lastHitboxSize = size;

            var hits = Physics2D.OverlapBoxAll(
                center,
                size,
                0f,
                hittableLayers | pogoSurfaceLayers
            );

            foreach (var col in hits)
            {
                if (!col || alreadyHit.Contains(col))
                    continue;

                // Don't hit yourself.
                if (col.transform.root == transform.root)
                    continue;

                alreadyHit.Add(col);

                bool didDamage = false;
                bool didPogo = false;

                // -------------------------------------------------
                // DAMAGE
                // -------------------------------------------------

                var dmg = col.GetComponentInParent<IDamageable>();

                if (dmg != null && dmg.IsAlive)
                {
                    Vector2 knockDir = DirVector(dir);

                    dmg.TakeDamage(
                        damage,
                        center,
                        knockDir
                    );

                    didDamage = true;

                    // Hit spark.
                    Vector3 sparkPos = col.ClosestPoint(transform.position);

                    HitSpark2D.Spawn(
                        sparkPos,
                        hitSparkColorA,
                        hitSparkColorB
                    );

                    // Enemy hit sound.
                    PlayRandomSFX(enemyHitSFX);
                }

                // -------------------------------------------------
                // POGO
                // -------------------------------------------------

                if (!pogoed &&
                    dir == AttackDir.Down &&
                    CanPogoOff(col))
                {
                    DoPogo();

                    pogoed = true;
                    didPogo = true;
                }

                // -------------------------------------------------
                // HIT SHAKE
                // -------------------------------------------------

                if ((didDamage || didPogo) &&
                    !didPogo &&
                    cam)
                {
                    cam.Shake(hitShake);
                }

                // -------------------------------------------------
                // HIT STOP
                // -------------------------------------------------

                if (didDamage || didPogo)
                {
                    if (hitStop > 0f)
                        StartCoroutine(HitStop());
                }
            }

            t += Time.deltaTime;

            yield return null;
        }

        isAttacking = false;
    }

    private bool CanPogoOff(Collider2D col)
    {
        // Pogo only works in the air.
        // This prevents infinite hopping on the ground.
        if (movement && movement.IsGrounded)
            return false;

        // Layer-based pogo surface.
        if (((1 << col.gameObject.layer) & pogoSurfaceLayers) != 0)
            return true;

        // Script-based pogo surface.
        var pogoable = col.GetComponentInParent<IPogoable>();

        return pogoable != null && pogoable.CanPogo;
    }

    private void DoPogo()
    {
        if (movement)
        {
            float boost =
                Mathf.Abs(rb.linearVelocity.x) > 0.1f
                    ? Mathf.Sign(rb.linearVelocity.x) * pogoMomentum
                    : 0f;

            movement.Pogo(
                pogoBounce,
                boost,
                pogoMaxHorizontal,
                pogoRefillsDash
            );
        }
        else if (rb)
        {
            // Unity 6 uses linearVelocity.
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                pogoBounce
            );
        }

        cooldownTimer = Mathf.Min(
            cooldownTimer,
            pogoCooldownOverride
        );

        // Pogo sound.
        PlayRandomSFX(pogoSFX);

        // Pogo camera shake.
        if (cam)
            cam.Shake(pogoShake);
    }

    private void PlayRandomSFX(AudioClip[] clips)
    {
        if (!hitAudioSource)
            return;

        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip;

        if (randomizeHitSFX)
        {
            clip = clips[Random.Range(0, clips.Length)];
        }
        else
        {
            clip = clips[0];
        }

        if (clip)
        {
            hitAudioSource.PlayOneShot(
                clip,
                hitSFXVolume
            );
        }
    }

    private IEnumerator HitStop()
    {
        float original = Time.timeScale;

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitStop);

        Time.timeScale = original;
    }

    private void GetHitbox(
        AttackDir dir,
        out Vector2 center,
        out Vector2 size)
    {
        Vector2 p = transform.position;

        switch (dir)
        {
            case AttackDir.Up:

                center = p + Vector2.up * upOffset;
                size = upSize;

                break;

            case AttackDir.Down:

                center = p + Vector2.down * downOffset;
                size = downSize;

                break;

            default:

                int f = movement
                    ? movement.Facing
                    : 1;

                center =
                    p +
                    Vector2.right *
                    f *
                    sideOffset;

                size = sideSize;

                break;
        }
    }

    private Vector2 DirVector(AttackDir dir)
    {
        switch (dir)
        {
            case AttackDir.Up:

                return Vector2.up;

            case AttackDir.Down:

                return Vector2.down;

            default:

                return Vector2.right *
                    (movement
                        ? movement.Facing
                        : 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawHitboxGizmo)
            return;

        Gizmos.color =
            new Color(
                1f,
                0.4f,
                0.2f,
                0.7f
            );

        Vector2 p = transform.position;

        // Down hitbox.
        Gizmos.DrawWireCube(
            p + Vector2.down * downOffset,
            downSize
        );

        // Up hitbox.
        Gizmos.DrawWireCube(
            p + Vector2.up * upOffset,
            upSize
        );

        // Side hitbox.
        int f =
            Application.isPlaying && movement
                ? movement.Facing
                : 1;

        Gizmos.DrawWireCube(
            p + Vector2.right * f * sideOffset,
            sideSize
        );
    }

    // KnockbackForce is applied by the enemy side;
    // exposed so enemies can read it.
    public float KnockbackForce => knockbackForce;
}
