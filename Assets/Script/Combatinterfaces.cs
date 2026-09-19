using UnityEngine;

/// <summary>Anything the player can hit with an attack.</summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int amount, Vector2 hitPoint, Vector2 knockbackDir);
}

/// <summary>
/// Anything the player can bounce off with a downward attack.
/// Enemies, spikes, breakable pots, projectiles — implement this and you can pogo it.
/// </summary>
public interface IPogoable
{
    bool CanPogo { get; }
}

/// <summary>
/// Implement on the player (or a component on it) to signal a temporary immunity window —
/// e.g. mid-slide or mid-dash — that contact-damage sources should respect.
/// </summary>
public interface IInvulnerable
{
    bool IsInvulnerable { get; }
}

/// <summary>
/// A player can end up with more than one IInvulnerable source (slide i-frames, hit i-frames,
/// dash i-frames, etc). This checks all of them so any single active source blocks damage,
/// instead of whichever GetComponentInParent happens to find first.
/// </summary>
public static class InvulnerabilityUtil
{
    public static bool IsInvulnerable(Component target)
    {
        var sources = target.GetComponentsInParent<IInvulnerable>();
        foreach (var s in sources)
            if (s.IsInvulnerable) return true;
        return false;
    }
}