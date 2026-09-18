using UnityEngine;

/// <summary>
/// Pure floating movement: gently drifts and bobs around its spawn point.
/// No dashing, no scaling, no rotation — just smooth idle motion.
/// Attach to the skull's GameObject. No Rigidbody2D needed.
/// </summary>
public class FloatingSkullMovement : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float driftRadius = 2.2f;
    [SerializeField] private float driftSpeed = 0.6f;
    [SerializeField] private float bobAmplitude = 0.35f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private float followLerp = 6f;

    private Vector2 homePoint;
    private float driftPhase;
    private float bobPhase;

    private void Awake()
    {
        homePoint = transform.position;
        driftPhase = Random.value * Mathf.PI * 2f;
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        driftPhase += Time.deltaTime * driftSpeed;
        bobPhase += Time.deltaTime * bobSpeed;

        Vector2 drift = new Vector2(
            Mathf.Cos(driftPhase) * driftRadius,
            Mathf.Sin(driftPhase * 1.7f) * driftRadius * 0.5f
        );
        float bob = Mathf.Sin(bobPhase) * bobAmplitude;

        Vector2 target = homePoint + drift + Vector2.up * bob;
        transform.position = Vector2.Lerp(
            transform.position,
            target,
            1f - Mathf.Exp(-followLerp * Time.deltaTime)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? (Vector3)homePoint : transform.position;
        Gizmos.color = new Color(0.5f, 1f, 0.7f, 0.5f);
        Gizmos.DrawWireSphere(origin, driftRadius);
    }
}