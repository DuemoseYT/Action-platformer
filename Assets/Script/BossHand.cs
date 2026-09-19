using UnityEngine;

/// <summary>
/// Put this on each hand's trigger-collider object (the collider you already made should
/// have Is Trigger checked). Deals damage to the player while damageActive is true.
///
/// Two ways to time it:
///  - Leave damageActive on (default) and it hurts the player any time it overlaps them —
///    simplest, works immediately with no extra setup.
///  - For precision, add Animation Events on your attack clip that call EnableHit() at the
///    moment the hand actually swings/slams, and DisableHit() right after, so a resting or
///    retracting hand can't accidentally hit the player.
/// </summary>
public class BossHand : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float knockbackForce = 16f;
    [Tooltip("Time before this hand can hit the player again, even while still overlapping.")]
    public float hitCooldown = 0.5f;
    [Tooltip("On by default so it works out of the box. Drive with EnableHit/DisableHit via Animation Events for precise timing instead.")]
    public bool damageActive = true;

    private float cooldownTimer;

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!damageActive || cooldownTimer > 0f) return;
        if (!other.CompareTag("Player")) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null || !dmg.IsAlive) return;
        if (InvulnerabilityUtil.IsInvulnerable(other)) return;

        cooldownTimer = hitCooldown;

        Vector2 dir = (Vector2)other.transform.position - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
        dmg.TakeDamage(damage, transform.position, dir.normalized);
    }

    // ── hook these up as Animation Events on your attack clip, at the exact frames
    //    where the hand should become dangerous / stop being dangerous ──
    public void EnableHit()  => damageActive = true;
    public void DisableHit() => damageActive = false;
}