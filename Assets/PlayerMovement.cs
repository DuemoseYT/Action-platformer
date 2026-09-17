using UnityEngine;

/// <summary>
/// Fast-paced 2D platformer controller.
/// WASD move, Space jump, Shift dash (8-directional), S/Ctrl slide, wall slide + wall jump, S (air) ground pound.
/// Includes coyote time, jump buffering, variable jump height, apex hang time and momentum conservation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // UNITY 6 NOTE: Rigidbody2D.velocity was renamed to linearVelocity.
    // If you get a deprecation warning/error, this property is the ONLY
    // place you need to change it (swap `velocity` for `linearVelocity`).
    // ─────────────────────────────────────────────────────────────
    private Vector2 Vel
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }

    [Header("Input Keys")]
    public KeyCode keyLeft  = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;
    public KeyCode keyUp    = KeyCode.W;
    public KeyCode keyDown  = KeyCode.S;
    public KeyCode keyJump  = KeyCode.Space;
    public KeyCode keyDash  = KeyCode.LeftShift;
    public KeyCode keySlide = KeyCode.LeftControl;   // S also works

    [Header("Run")]
    public float maxSpeed      = 14f;
    public float acceleration  = 95f;
    public float deceleration  = 75f;
    [Range(0f, 1f)] public float airAccelMult = 0.65f;
    [Range(0f, 1f)] public float airDecelMult = 0.4f;
    [Tooltip("1 = linear. Below 1 = snappier response at low speed.")]
    public float velPower = 0.9f;
    [Tooltip("Don't fight extra speed gained from dashes / wall jumps.")]
    public bool conserveMomentum = true;

    [Header("Jump")]
    public float jumpPower  = 19f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;
    [Range(0f, 1f)] public float jumpCutMult = 0.45f;

    [Header("Gravity")]
    public float gravityScale    = 5f;
    public float fallGravityMult = 1.55f;
    [Tooltip("Extra gravity while holding S in the air.")]
    public float fastFallMult    = 2.2f;
    [Tooltip("Reduced gravity near the top of the arc = floaty hang time.")]
    public float apexGravityMult = 0.6f;
    public float apexThreshold   = 3.5f;
    public float maxFallSpeed    = 32f;

    [Header("Dash")]
    public float dashSpeed    = 34f;
    public float dashDuration = 0.14f;
    [Tooltip("Speed you're left with the instant the dash ends.")]
    public float dashEndSpeed = 16f;
    public float dashCooldown = 0.28f;
    public int   maxDashes    = 1;

    [Header("Wall")]
    public float   wallSlideSpeed   = 4.5f;
    public Vector2 wallJumpPower    = new Vector2(17f, 19f);
    [Tooltip("Horizontal input is ignored this long after a wall jump so you actually leave the wall.")]
    public float   wallJumpLockTime = 0.16f;
    public float   wallCoyoteTime   = 0.12f;

    [Header("Slide")]
    public float slideBoost    = 19f;
    public float slideFriction = 9f;
    public float slideMinSpeed = 6f;
    public float slideMaxTime  = 1.2f;
    [Tooltip("Horizontal speed multiplier when you jump out of a slide.")]
    public float slideJumpBoost = 1.15f;
    [Tooltip("Optional: collider shrunk while sliding so you fit under gaps.")]
    public CapsuleCollider2D bodyCollider;
    [Range(0.2f, 1f)] public float slideHeightMult = 0.55f;

    [Header("Ground Pound")]
    public KeyCode keyGroundPound = KeyCode.S;
    public float groundPoundSpeed  = 40f;
    [Tooltip("Locks horizontal movement while pounding. Turn off for a more Hollow-Knight-ish diagonal pound.")]
    public bool  groundPoundLockX  = true;
    public int    groundPoundDamage    = 2;
    public float  groundPoundRadius    = 2.2f;
    public float  groundPoundKnockback = 14f;
    public LayerMask groundPoundHitLayers;
    public float  groundPoundShake = 0.22f;
    public Color  groundPoundDustA = new Color(0.75f, 0.65f, 0.5f);
    public Color  groundPoundDustB = new Color(0.45f, 0.35f, 0.25f);
    public CameraFollow2D cam;   // optional, for landing shake
    public PlayerSFX sfx;        // optional, for jump/dash/slide/pound sounds

    [Header("Collision Checks")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2   groundCheckSize = new Vector2(0.5f, 0.12f);
    public Transform wallCheck;
    public Vector2   wallCheckSize   = new Vector2(0.12f, 0.9f);
    public float     wallCheckOffset = 0.45f;
    public Transform ceilingCheck;
    public Vector2   ceilingCheckSize = new Vector2(0.45f, 0.12f);

    [Header("Visuals (optional)")]
    public SpriteRenderer spriteRenderer;

    // ── runtime state ────────────────────────────────────────────
    private Rigidbody2D rb;

    private float moveX, moveY;
    private int   facing = 1;

    private bool isGrounded, onWallLeft, onWallRight;
    private bool IsOnWall => onWallLeft || onWallRight;
    private int  WallDir  => onWallRight ? 1 : (onWallLeft ? -1 : 0);

    private float coyoteCounter, bufferCounter, wallCoyoteCounter;
    private int   lastWallDir;
    private bool  isJumping, jumpCutApplied;

    private bool  isDashing;
    private float dashTimer, dashCooldownTimer;
    private int   dashesLeft;
    private Vector2 dashDir;

    private bool  isSliding;
    private float slideTimer;
    private float wallJumpLockTimer;

    private Vector2 baseColliderSize;
    private Vector2 baseColliderOffset;

    private bool isGroundPounding;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!bodyCollider)   bodyCollider   = GetComponent<CapsuleCollider2D>();
        if (bodyCollider)
        {
            baseColliderSize   = bodyCollider.size;
            baseColliderOffset = bodyCollider.offset;
        }
        dashesLeft = maxDashes;
        if (!cam && Camera.main) cam = Camera.main.GetComponent<CameraFollow2D>();
        if (!sfx) sfx = GetComponent<PlayerSFX>();
    }

    private void Update()
    {
        ReadInput();
        RunChecks();
        TickTimers();

        // ── jump ───────────────────────────────────────────────
        if (Input.GetKeyDown(keyJump)) bufferCounter = jumpBuffer;

        if (bufferCounter > 0f && !isDashing)
        {
            if (coyoteCounter > 0f)          Jump();
            else if (wallCoyoteCounter > 0f) WallJump();
        }

        // variable jump height: releasing early cuts the rise
        if (Input.GetKeyUp(keyJump) && Vel.y > 0f && isJumping && !jumpCutApplied)
        {
            Vel = new Vector2(Vel.x, Vel.y * jumpCutMult);
            jumpCutApplied = true;
        }
        if (Vel.y <= 0f) isJumping = false;

        // ── dash ───────────────────────────────────────────────
        if (Input.GetKeyDown(keyDash) && dashesLeft > 0 && dashCooldownTimer <= 0f && !isDashing)
            StartDash();

        // ── ground pound ────────────────────────────────────────
        if (Input.GetKeyDown(keyGroundPound) && !isGrounded && !isDashing && !isSliding && !isGroundPounding)
            StartGroundPound();

        // ── slide ──────────────────────────────────────────────
        bool slideHeld = Input.GetKey(keySlide) || Input.GetKey(keyDown);
        if (slideHeld && !isSliding && !isDashing && isGrounded && Mathf.Abs(Vel.x) >= slideMinSpeed)
            StartSlide();
        if (isSliding && (!slideHeld || slideTimer > slideMaxTime || Mathf.Abs(Vel.x) < slideMinSpeed * 0.5f))
            TryEndSlide();

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isDashing)        { DoDash();  return; }
        if (isGroundPounding) { Vel = new Vector2(groundPoundLockX ? 0f : Vel.x, -groundPoundSpeed); return; }
        if (isSliding)        { DoSlide();          }
        else                  { DoRun();            }

        DoWallSlide();
        DoGravity();
    }

    // ── input & checks ───────────────────────────────────────────

    private void ReadInput()
    {
        moveX = (Input.GetKey(keyRight) ? 1 : 0) - (Input.GetKey(keyLeft) ? 1 : 0);
        moveY = (Input.GetKey(keyUp)    ? 1 : 0) - (Input.GetKey(keyDown) ? 1 : 0);
    }

    private void RunChecks()
    {
        bool wasGrounded = isGrounded;
        isGrounded = groundCheck && Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (wallCheck)
        {
            Vector2 p = wallCheck.position;
            onWallRight = Physics2D.OverlapBox(p + Vector2.right * wallCheckOffset, wallCheckSize, 0f, groundLayer);
            onWallLeft  = Physics2D.OverlapBox(p + Vector2.left  * wallCheckOffset, wallCheckSize, 0f, groundLayer);
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            dashesLeft = maxDashes;
            if (!wasGrounded) isJumping = false;
            if (isGroundPounding) EndGroundPound();
        }

        if (IsOnWall && !isGrounded)
        {
            wallCoyoteCounter = wallCoyoteTime;
            lastWallDir = WallDir;
            dashesLeft = maxDashes;
        }
    }

    private void TickTimers()
    {
        float dt = Time.deltaTime;
        coyoteCounter     -= dt;
        bufferCounter     -= dt;
        wallCoyoteCounter -= dt;
        dashCooldownTimer -= dt;
        wallJumpLockTimer -= dt;
        if (isSliding) slideTimer += dt;
    }

    private void UpdateFacing()
    {
        if (isDashing || isSliding) return;
        if (moveX != 0) facing = (int)Mathf.Sign(moveX);
        if (spriteRenderer) spriteRenderer.flipX = facing < 0;
    }

    // ── run ──────────────────────────────────────────────────────

    private void DoRun()
    {
        // horizontal input is briefly ignored after a wall jump
        float input = wallJumpLockTimer > 0f ? 0f : moveX;

        float targetSpeed = input * maxSpeed;
        float speedDif    = targetSpeed - Vel.x;

        float accelRate;
        if (isGrounded)
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        else
            accelRate = Mathf.Abs(targetSpeed) > 0.01f
                      ? acceleration * airAccelMult
                      : deceleration * airDecelMult;

        // keep speed gained from dashes / wall jumps instead of braking to maxSpeed
        if (conserveMomentum
            && Mathf.Abs(Vel.x) > Mathf.Abs(targetSpeed)
            && Mathf.Sign(Vel.x) == Mathf.Sign(targetSpeed)
            && Mathf.Abs(targetSpeed) > 0.01f
            && !isGrounded)
            accelRate = 0f;

        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);
        rb.AddForce(movement * Vector2.right);
    }

    // ── jumping ──────────────────────────────────────────────────

    private void Jump()
    {
        bufferCounter = 0f;
        coyoteCounter = 0f;
        isJumping = true;
        jumpCutApplied = false;

        float xBoost = 1f;
        if (isSliding) { TryEndSlide(true); xBoost = slideJumpBoost; }

        Vel = new Vector2(Vel.x * xBoost, jumpPower);
        sfx?.PlayJump();
    }

    private void WallJump()
    {
        int dir = WallDir != 0 ? WallDir : lastWallDir;

        bufferCounter = 0f;
        wallCoyoteCounter = 0f;
        isJumping = true;
        jumpCutApplied = false;
        wallJumpLockTimer = wallJumpLockTime;

        Vel = new Vector2(-dir * wallJumpPower.x, wallJumpPower.y);
        facing = -dir;
        if (spriteRenderer) spriteRenderer.flipX = facing < 0;
        sfx?.PlayWallJump();
    }

    // ── dash ─────────────────────────────────────────────────────

    private void StartDash()
    {
        Vector2 dir = new Vector2(moveX, moveY);
        if (dir.sqrMagnitude < 0.01f) dir = new Vector2(facing, 0f);
        dashDir = dir.normalized;

        isDashing = true;
        dashTimer = 0f;
        dashesLeft--;
        dashCooldownTimer = dashCooldown;

        if (isSliding) TryEndSlide(true);
        rb.gravityScale = 0f;
        sfx?.PlayDash();
    }

    private void DoDash()
    {
        Vel = dashDir * dashSpeed;
        dashTimer += Time.fixedDeltaTime;
        if (dashTimer >= dashDuration) EndDash();
    }

    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = gravityScale;
        Vel = dashDir * dashEndSpeed;
    }

    // ── slide ────────────────────────────────────────────────────

    private void StartSlide()
    {
        isSliding  = true;
        slideTimer = 0f;
        sfx?.StartSlideLoop();

        float dir = Mathf.Sign(Vel.x);
        Vel = new Vector2(dir * Mathf.Max(Mathf.Abs(Vel.x), slideBoost), Vel.y);

        if (bodyCollider)
        {
            bodyCollider.size   = new Vector2(baseColliderSize.x, baseColliderSize.y * slideHeightMult);
            bodyCollider.offset = new Vector2(baseColliderOffset.x,
                                              baseColliderOffset.y - (baseColliderSize.y - bodyCollider.size.y) * 0.5f);
        }
    }

    /// <summary>Ends the slide unless there's a ceiling overhead. Pass true to force it.</summary>
    private void TryEndSlide(bool force = false)
    {
        if (!force && ceilingCheck &&
            Physics2D.OverlapBox(ceilingCheck.position, ceilingCheckSize, 0f, groundLayer))
            return;

        isSliding = false;
        sfx?.StopSlideLoop();
        if (bodyCollider)
        {
            bodyCollider.size   = baseColliderSize;
            bodyCollider.offset = baseColliderOffset;
        }
    }

    private void DoSlide()
    {
        // no acceleration while sliding, just friction — momentum is the whole point
        float newX = Mathf.MoveTowards(Vel.x, 0f, slideFriction * Time.fixedDeltaTime);
        Vel = new Vector2(newX, Vel.y);
    }

    // ── wall slide & gravity ─────────────────────────────────────

    private void DoWallSlide()
    {
        bool pressingIntoWall = (onWallLeft && moveX < 0) || (onWallRight && moveX > 0);
        if (pressingIntoWall && !isGrounded && Vel.y < 0f)
            Vel = new Vector2(Vel.x, Mathf.Max(Vel.y, -wallSlideSpeed));
    }

    private void DoGravity()
    {
        if (Vel.y < 0f)
        {
            bool fastFall = moveY < 0 && !isGrounded;
            rb.gravityScale = gravityScale * (fastFall ? fastFallMult : fallGravityMult);
        }
        else if (Mathf.Abs(Vel.y) < apexThreshold && !isGrounded)
        {
            rb.gravityScale = gravityScale * apexGravityMult;   // hang time at the apex
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        if (Vel.y < -maxFallSpeed) Vel = new Vector2(Vel.x, -maxFallSpeed);
    }

    // ── ground pound ─────────────────────────────────────────────

    private void StartGroundPound()
    {
        isGroundPounding = true;
        if (isDashing) EndDash();
        if (IsOnWall)  { onWallLeft = onWallRight = false; }   // don't let wall-slide fight the slam

        rb.gravityScale = 0f;
        Vel = new Vector2(groundPoundLockX ? 0f : Vel.x, -groundPoundSpeed);
    }

    private void EndGroundPound()
    {
        isGroundPounding = false;
        rb.gravityScale = gravityScale;
        Vel = new Vector2(Vel.x, 0f);

        var hits = Physics2D.OverlapCircleAll(transform.position, groundPoundRadius, groundPoundHitLayers);
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            var dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg == null || !dmg.IsAlive) continue;

            Vector2 dir = (Vector2)hit.transform.position - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            dmg.TakeDamage(groundPoundDamage, transform.position, dir.normalized);
        }

        if (cam) cam.Shake(groundPoundShake);
        sfx?.PlayGroundPoundLand();
        HitSpark2D.Spawn(groundCheck ? groundCheck.position : transform.position,
                          groundPoundDustA, groundPoundDustB, burstCount: 20, size: 0.2f);
    }

    // ── pogo support ─────────────────────────────────────────────
    public bool IsGrounded => isGrounded;
    public bool IsSliding  => isSliding;
    public int  Facing     => facing;

    /// <summary>Bounce the player upward off something they down-attacked.</summary>
    public void Pogo(float bouncePower, float horizontalBoost, float maxHorizontal, bool refillDash)
    {
        if (isDashing) EndDash();
        if (isSliding) TryEndSlide(true);

        isJumping      = true;
        jumpCutApplied = false;          // so you can still cut the bounce short
        coyoteCounter  = 0f;
        wallCoyoteCounter = 0f;
        if (refillDash) dashesLeft = maxDashes;

        float newX = Mathf.Clamp(Vel.x + horizontalBoost, -maxHorizontal, maxHorizontal);
        Vel = new Vector2(newX, Mathf.Max(Vel.y, 0f) + bouncePower);
    }

    // ── gizmos ───────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck)  Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        if (ceilingCheck) Gizmos.DrawWireCube(ceilingCheck.position, ceilingCheckSize);

        Gizmos.color = Color.cyan;
        if (wallCheck)
        {
            Vector3 p = wallCheck.position;
            Gizmos.DrawWireCube(p + Vector3.right * wallCheckOffset, wallCheckSize);
            Gizmos.DrawWireCube(p + Vector3.left  * wallCheckOffset, wallCheckSize);
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, groundPoundRadius);
    }
}