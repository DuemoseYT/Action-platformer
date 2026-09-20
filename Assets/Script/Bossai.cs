using UnityEngine;

/// <summary>
/// Minimal boss "brain": when the player is within range and the cooldown has elapsed,
/// tell BossController to fire the double hand attack. Swap this out for a real state
/// machine later — this exists so you have something calling DoubleHandAttack() at all.
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("References")]
    public BossController controller;
    public Transform player;   // leave empty to auto-find by tag on Start

    [Header("Attack Trigger")]
    public float attackRange = 8f;
    public float minCooldown = 2f;
    public float maxCooldown = 4f;
    [Tooltip("If false, the boss attacks regardless of distance once the cooldown is up.")]
    public bool requireRange = true;

    [Header("Attack Mix (relative weights — don't need to add up to any particular total)")]
    public float doubleAttackWeight = 1f;
    public float groundSlamWeight   = 1f;
    public float leftSweepWeight    = 1f;
    public float rightSweepWeight   = 1f;

    private float cooldownTimer;

    private void Awake()
    {
        if (!controller) controller = GetComponent<BossController>();
    }

    private void Start()
    {
        if (!player)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found) player = found.transform;
        }
        cooldownTimer = Random.Range(minCooldown, maxCooldown);
    }

    private void Update()
    {
        if (!controller) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (requireRange && player)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > attackRange) return;
        }

        DoRandomAttack();
        cooldownTimer = Random.Range(minCooldown, maxCooldown);
    }

    private void DoRandomAttack()
    {
        float total = doubleAttackWeight + groundSlamWeight + leftSweepWeight + rightSweepWeight;
        if (total <= 0f) { controller.DoubleHandAttack(); return; }

        float roll = Random.value * total;
        if ((roll -= doubleAttackWeight) < 0f) { controller.DoubleHandAttack(); return; }
        if ((roll -= groundSlamWeight)   < 0f) { controller.GroundSlamAttack(); return; }
        if ((roll -= leftSweepWeight)    < 0f) { controller.LeftHandSweepAttack(); return; }
        controller.RightHandSweepAttack();
    }

    private void OnDrawGizmosSelected()
    {
        if (!requireRange) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}