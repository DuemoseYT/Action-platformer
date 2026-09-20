using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates the boss's two hands so their attack animations play together. Each hand
/// keeps its own Animator and its own animation clip — this just tells both to start,
/// either on the exact same frame or with a small stagger for a one-two feel instead of
/// a perfect mirror. Call DoubleHandAttack() from wherever your boss decides to attack
/// (a state machine, an Animation Event on the head, a timer — whatever you're using).
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("Hands")]
    public Animator leftHandAnimator;
    public Animator rightHandAnimator;
    [Tooltip("Trigger parameter name in both hands' Animator Controllers.")]
    public string attackTrigger = "Attack";
    [Tooltip("Trigger parameter name for the ground slam animation on both hands.")]
    public string groundSlamTrigger = "GroundSlam";
    [Tooltip("0 = both hands start on the exact same frame. Above 0 = left hand leads by this many seconds.")]
    public float handStagger = 0f;

    [Header("Hand Sweep")]
    public BossHandSweep leftHandSweep;
    public BossHandSweep rightHandSweep;
    public Color sweepTelegraphColor = new Color(1f, 0.85f, 0.2f, 0.85f);
    [Tooltip("How many warning rings are spaced along the sweep path.")]
    public int sweepTelegraphCount = 5;

    [Header("Simple Auto-Attack (optional — handy for testing before you wire up real AI)")]
    public bool autoAttack = false;
    public float attackInterval = 3f;

    [Header("Telegraph")]
    [Tooltip("How long the warning ring shows before the attack actually fires.")]
    public float telegraphTime = 0.5f;
    public Color attackTelegraphColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    public Color slamTelegraphColor = new Color(1f, 0.6f, 0.1f, 0.85f);

    private void Start()
    {
        if (autoAttack) StartCoroutine(AutoAttackLoop());
    }

    private IEnumerator AutoAttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            DoubleHandAttack();
        }
    }

    /// <summary>Fire both hands' attack animation together.</summary>
    public void DoubleHandAttack() => StartCoroutine(DoubleAttackRoutine());

    /// <summary>Fire both hands' ground slam animation together. The slam clip's impact
    /// frame should call BossGroundSlam.SpawnShockwave() via an Animation Event.</summary>
    public void GroundSlamAttack() => StartCoroutine(GroundSlamRoutine());

    private IEnumerator GroundSlamRoutine()
    {
        if (leftHandAnimator)  AttackTelegraph.Show(leftHandAnimator.transform.position, telegraphTime, slamTelegraphColor);
        if (rightHandAnimator) AttackTelegraph.Show(rightHandAnimator.transform.position, telegraphTime, slamTelegraphColor);
        yield return new WaitForSeconds(telegraphTime);

        if (leftHandAnimator) leftHandAnimator.SetTrigger(groundSlamTrigger);
        if (handStagger > 0f) yield return new WaitForSeconds(handStagger);
        if (rightHandAnimator) rightHandAnimator.SetTrigger(groundSlamTrigger);
    }

    private IEnumerator DoubleAttackRoutine()
    {
        if (leftHandAnimator)  AttackTelegraph.Show(leftHandAnimator.transform.position, telegraphTime, attackTelegraphColor);
        if (rightHandAnimator) AttackTelegraph.Show(rightHandAnimator.transform.position, telegraphTime, attackTelegraphColor);
        yield return new WaitForSeconds(telegraphTime);

        if (leftHandAnimator) leftHandAnimator.SetTrigger(attackTrigger);
        if (handStagger > 0f) yield return new WaitForSeconds(handStagger);
        if (rightHandAnimator) rightHandAnimator.SetTrigger(attackTrigger);
    }

    /// <summary>For an alternating pattern instead of a double attack.</summary>
    public void LeftHandAttack()  { if (leftHandAnimator)  leftHandAnimator.SetTrigger(attackTrigger); }
    public void RightHandAttack() { if (rightHandAnimator) rightHandAnimator.SetTrigger(attackTrigger); }

    /// <summary>Left hand sweeps across the room.</summary>
    public void LeftHandSweepAttack() => StartCoroutine(SweepRoutine(leftHandSweep));

    /// <summary>Right hand sweeps across the room.</summary>
    public void RightHandSweepAttack() => StartCoroutine(SweepRoutine(rightHandSweep));

    private IEnumerator SweepRoutine(BossHandSweep hand)
    {
        if (!hand) yield break;

        if (hand.startPoint && hand.endPoint)
        {
            for (int i = 0; i < sweepTelegraphCount; i++)
            {
                float p = sweepTelegraphCount > 1 ? (float)i / (sweepTelegraphCount - 1) : 0f;
                Vector3 pos = Vector3.Lerp(hand.startPoint.position, hand.endPoint.position, p);
                AttackTelegraph.Show(pos, telegraphTime, sweepTelegraphColor);
            }
        }

        yield return new WaitForSeconds(telegraphTime);
        hand.TriggerSweep();
    }
}