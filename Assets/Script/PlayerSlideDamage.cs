using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deals damage to anything you slide into. Grants brief invulnerability while sliding
/// (via IInvulnerable) so contact damage doesn't fire back at you the instant you plow
/// through an enemy. Each enemy is hit once per slide, not once per physics frame.
/// </summary>
public class PlayerSlideDamage : MonoBehaviour, IInvulnerable
{
    [Header("References")]
    public PlayerMovement2D movement;
    public CameraFollow2D cam;   // optional, for a small hit shake
    public PlayerSFX sfx;        // optional, for the slide-hit sound

    [Header("Damage")]
    public int   damage = 1;
    public float knockbackForce = 10f;
    public LayerMask hittableLayers;

    [Header("Juice")]
    public float hitShake = 0.05f;
    public Color sparkColorA = new Color(1f, 0.95f, 0.4f);
    public Color sparkColorB = new Color(1f, 0.35f, 0.1f);

    public bool IsInvulnerable => movement && movement.IsSliding;

    private readonly HashSet<Collider2D> hitThisSlide = new HashSet<Collider2D>();
    private bool wasSliding;

    private void Awake()
    {
        if (!movement) movement = GetComponent<PlayerMovement2D>();
        if (!cam && Camera.main) cam = Camera.main.GetComponent<CameraFollow2D>();
        if (!sfx) sfx = GetComponent<PlayerSFX>();
    }

    private void Update()
    {
        bool sliding = movement && movement.IsSliding;
        if (sliding && !wasSliding) hitThisSlide.Clear();   // fresh slide = fresh hit list
        wasSliding = sliding;
    }

    private void OnTriggerEnter2D(Collider2D other)  => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)   => TryHit(other);
    private void OnCollisionEnter2D(Collision2D col) => TryHit(col.collider);
    private void OnCollisionStay2D(Collision2D col)  => TryHit(col.collider);

    private void TryHit(Collider2D other)
    {
        if (!IsInvulnerable) return;
        if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;
        if (other.transform.root == transform.root) return;
        if (hitThisSlide.Contains(other)) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null || !dmg.IsAlive) return;

        hitThisSlide.Add(other);

        Vector2 dir = (Vector2)other.transform.position - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right * (movement ? movement.Facing : 1);
        dmg.TakeDamage(damage, transform.position, dir.normalized);

        HitSpark2D.Spawn(other.ClosestPoint(transform.position), sparkColorA, sparkColorB);
        if (cam) cam.Shake(hitShake);
        sfx?.PlaySlideHit();
    }
}