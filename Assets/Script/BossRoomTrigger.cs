using UnityEngine;

/// <summary>
/// Put this on your boss-room entry trigger (Is Trigger checked on its Collider2D).
/// When the player enters, the camera stops following them and pans/zooms out to
/// frame the whole room instead. Call UnlockRoom() (e.g. from the boss's onDeath
/// event) to hand control back to normal follow.
/// </summary>
public class BossRoomTrigger : MonoBehaviour
{
    [Tooltip("Auto-finds CameraFollow2D on Camera.main if left empty.")]
    public CameraFollow2D cam;
    [Tooltip("Defines the room's extents. Leave empty to use this object's own collider.")]
    public Collider2D roomBounds;
    [Tooltip("Only fires once. Turn off if you want it to re-lock every time the player walks in.")]
    public bool oneShot = true;
    [Tooltip("Resume following the player as soon as they leave the trigger, instead of waiting for UnlockRoom().")]
    public bool unlockOnExit = false;

    private bool triggered;

    private void Awake()
    {
        if (!cam && Camera.main) cam = Camera.main.GetComponent<CameraFollow2D>();
        if (!roomBounds) roomBounds = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneShot && triggered) return;
        triggered = true;

        if (cam && roomBounds) cam.LockToRoom(roomBounds.bounds);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!unlockOnExit || !other.CompareTag("Player")) return;
        UnlockRoom();
    }

    /// <summary>Hand the camera back to normal follow. Hook this to the boss's death event.</summary>
    public void UnlockRoom()
    {
        if (cam) cam.Unlock();
    }
}