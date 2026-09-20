using UnityEngine;

/// <summary>
/// Put this on each hand alongside BossHand. Call SpawnShockwave() from an Animation Event
/// at the exact frame the hand hits the floor in your slam animation, and it fires a
/// damaging wave traveling outward along the ground.
/// </summary>
public class BossGroundSlam : MonoBehaviour
{
    [Header("Shockwave")]
    [Tooltip("A GameObject with ShockwaveHazard on it (see setup notes). Create one, drag it into your Project window to make a prefab, then assign it here.")]
    public GameObject shockwavePrefab;
    [Tooltip("Where the wave spawns from. Defaults to this hand's own position.")]
    public Transform groundPoint;
    [Tooltip("Fire a wave both left AND right instead of just outward from boss center.")]
    public bool bothDirections = false;

    [Header("Tuning")]
    public int damage = 1;
    public float knockbackForce = 12f;
    public float travelSpeed = 10f;
    public float maxDistance = 14f;
    public LayerMask wallLayer;

    /// <summary>Hook this up as an Animation Event on the slam clip's impact frame.</summary>
    public void SpawnShockwave()
    {
        Vector3 origin = groundPoint ? groundPoint.position : transform.position;

        if (bothDirections)
        {
            Spawn(origin, Vector2.left);
            Spawn(origin, Vector2.right);
        }
        else
        {
            // fire away from the boss's center so waves from each hand converge outward from the middle
            float dir = Mathf.Sign(transform.position.x - transform.root.position.x);
            if (dir == 0f) dir = 1f;
            Spawn(origin, new Vector2(dir, 0f));
        }
    }

    private void Spawn(Vector3 origin, Vector2 direction)
    {
        if (!shockwavePrefab) return;

        var go = Instantiate(shockwavePrefab, origin, Quaternion.identity);
        var hazard = go.GetComponent<ShockwaveHazard>();
        if (hazard)
        {
            hazard.wallLayer = wallLayer;
            hazard.Init(direction, travelSpeed, damage, knockbackForce, maxDistance);
        }
    }
}