using UnityEngine;

/// <summary>
/// Smooth 2D follow camera built for fast movement: velocity look-ahead,
/// a vertical dead zone so jumps don't bounce the view, optional world bounds
/// and a shake you can trigger from dashes, landings or hits.
/// Put this on the Camera object and drag the player into Target.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Smoothing")]
    [Tooltip("Seconds to catch up horizontally. Lower = tighter.")]
    public float smoothX = 0.12f;
    [Tooltip("Vertical is usually slower so jumps feel calm.")]
    public float smoothY = 0.22f;
    [Tooltip("Cap on how fast the camera can travel. 0 = uncapped.")]
    public float maxSpeed = 0f;

    [Header("Look Ahead")]
    [Tooltip("How far ahead of the player the camera sits at full speed.")]
    public float lookAheadX = 2.5f;
    public float lookAheadY = 1.2f;
    [Tooltip("Player speed at which look-ahead is maxed out.")]
    public float lookAheadRefSpeed = 16f;
    public float lookAheadSmooth = 0.25f;
    [Tooltip("Only look ahead vertically when falling fast (nice for long drops).")]
    public bool verticalLookAheadOnFallOnly = true;

    [Header("Dead Zone")]
    [Tooltip("Vertical band the player can move in without the camera reacting.")]
    public float deadZoneY = 1.6f;
    public float deadZoneX = 0f;

    [Header("World Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 boundsMin = new Vector2(-50f, -10f);
    public Vector2 boundsMax = new Vector2(50f, 20f);

    [Header("Shake")]
    public float shakeDecay = 4f;

    private Camera cam;
    private Rigidbody2D targetRb;
    private Vector3 velRef;
    private Vector2 lookAhead, lookAheadRef;
    private float shakeAmount;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (target) targetRb = target.GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (target) transform.position = ClampToBounds(TargetPoint());
    }

    private void LateUpdate()
    {
        if (!target) return;
        if (!targetRb) targetRb = target.GetComponent<Rigidbody2D>();

        UpdateLookAhead();

        Vector3 desired = TargetPoint();
        Vector3 pos = transform.position;

        // dead zone: ignore small movements inside the band
        float dx = desired.x - pos.x;
        float dy = desired.y - pos.y;
        if (Mathf.Abs(dx) < deadZoneX) desired.x = pos.x;
        else desired.x -= Mathf.Sign(dx) * deadZoneX;
        if (Mathf.Abs(dy) < deadZoneY) desired.y = pos.y;
        else desired.y -= Mathf.Sign(dy) * deadZoneY;

        // smooth each axis separately
        float newX = Mathf.SmoothDamp(pos.x, desired.x, ref velRef.x, smoothX,
                                      maxSpeed > 0f ? maxSpeed : Mathf.Infinity, Time.deltaTime);
        float newY = Mathf.SmoothDamp(pos.y, desired.y, ref velRef.y, smoothY,
                                      maxSpeed > 0f ? maxSpeed : Mathf.Infinity, Time.deltaTime);

        Vector3 result = ClampToBounds(new Vector3(newX, newY, pos.z));

        // shake goes on last so it isn't smoothed or clamped away
        if (shakeAmount > 0.0001f)
        {
            result += (Vector3)(Random.insideUnitCircle * shakeAmount);
            shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, shakeDecay * shakeAmount * Time.deltaTime + 0.01f * Time.deltaTime);
        }

        transform.position = result;
    }

    private Vector3 TargetPoint()
    {
        Vector2 p = (Vector2)target.position + offset + lookAhead;
        return new Vector3(p.x, p.y, transform.position.z == 0f ? -10f : transform.position.z);
    }

    private void UpdateLookAhead()
    {
        Vector2 wanted = Vector2.zero;

        if (targetRb)
        {
            // UNITY 6: swap `velocity` for `linearVelocity` here if needed.
            Vector2 v = targetRb.linearVelocity;

            wanted.x = Mathf.Clamp(v.x / lookAheadRefSpeed, -1f, 1f) * lookAheadX;

            float vy = v.y;
            if (verticalLookAheadOnFallOnly && vy > 0f) vy = 0f;
            wanted.y = Mathf.Clamp(vy / lookAheadRefSpeed, -1f, 1f) * lookAheadY;
        }

        lookAhead = Vector2.SmoothDamp(lookAhead, wanted, ref lookAheadRef, lookAheadSmooth, Mathf.Infinity, Time.deltaTime);
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (!useBounds || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = boundsMin.x + halfW, maxX = boundsMax.x - halfW;
        float minY = boundsMin.y + halfH, maxY = boundsMax.y - halfH;

        pos.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    // ── public API ───────────────────────────────────────────────

    /// <summary>Call from the player on dash, landing, hit, etc.</summary>
    public void Shake(float amount)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
    }

    /// <summary>Jump the camera straight to the target (room transitions, respawn).</summary>
    public void SnapToTarget()
    {
        if (!target) return;
        lookAhead = Vector2.zero;
        velRef = Vector3.zero;
        transform.position = ClampToBounds(TargetPoint());
    }

    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 c = (boundsMin + boundsMax) * 0.5f;
            Vector3 s = boundsMax - boundsMin;
            Gizmos.DrawWireCube(c, s);
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(deadZoneX * 2f, 0.05f),
                                                           Mathf.Max(deadZoneY * 2f, 0.05f), 0.1f));
    }
}