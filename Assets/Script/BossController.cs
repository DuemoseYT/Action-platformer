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
    [Tooltip("0 = both hands start on the exact same frame. Above 0 = left hand leads by this many seconds.")]
    public float handStagger = 0f;

    [Header("Simple Auto-Attack (optional — handy for testing before you wire up real AI)")]
    public bool autoAttack = false;
    public float attackInterval = 3f;

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

    private IEnumerator DoubleAttackRoutine()
    {
        if (leftHandAnimator) leftHandAnimator.SetTrigger(attackTrigger);
        if (handStagger > 0f) yield return new WaitForSeconds(handStagger);
        if (rightHandAnimator) rightHandAnimator.SetTrigger(attackTrigger);
    }

    /// <summary>For an alternating pattern instead of a double attack.</summary>
    public void LeftHandAttack()  { if (leftHandAnimator)  leftHandAnimator.SetTrigger(attackTrigger); }
    public void RightHandAttack() { if (rightHandAnimator) rightHandAnimator.SetTrigger(attackTrigger); }
}