using System.Collections;
using UnityEngine;

/// <summary>
/// Sweeps this hand across the room in one wide horizontal pass — like wiping dust off a
/// counter. Place two empty Transforms marking where the sweep starts and ends (usually
/// just past the room's left/right edges) and assign them below. Damage is still handled
/// by this hand's existing BossHand component; this script only moves it.
/// </summary>
[RequireComponent(typeof(Animator))]
public class BossHandSweep : MonoBehaviour
{
    [Header("Sweep Path")]
    public Transform startPoint;
    public Transform endPoint;
    public float sweepDuration = 0.9f;
    public AnimationCurve sweepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Return")]
    [Tooltip("Move back to the hand's original position after the sweep finishes.")]
    public bool returnAfterSweep = true;
    public float returnDuration = 0.5f;

    [Header("Animation (optional)")]
    [Tooltip("Trigger fired on this hand's Animator when the sweep starts, for an arm-swing pose. Leave empty to skip.")]
    public string sweepAnimTrigger = "Sweep";

    private Animator animator;
    private Vector3 restPosition;
    private Coroutine routine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        restPosition = transform.position;
    }

    /// <summary>Start the sweep from startPoint to endPoint.</summary>
    public void TriggerSweep()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        if (!string.IsNullOrEmpty(sweepAnimTrigger)) animator.SetTrigger(sweepAnimTrigger);

        Vector3 from = startPoint ? startPoint.position : restPosition;
        Vector3 to   = endPoint   ? endPoint.position   : restPosition;
        transform.position = from;

        float t = 0f;
        while (t < sweepDuration)
        {
            t += Time.deltaTime;
            float p = sweepCurve.Evaluate(Mathf.Clamp01(t / sweepDuration));
            transform.position = Vector3.Lerp(from, to, p);
            yield return null;
        }
        transform.position = to;

        if (returnAfterSweep)
        {
            Vector3 returnFrom = transform.position;
            t = 0f;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(returnFrom, restPosition, t / returnDuration);
                yield return null;
            }
            transform.position = restPosition;
        }
    }
}