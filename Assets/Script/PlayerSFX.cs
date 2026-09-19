using System.Collections;
using UnityEngine;

/// <summary>
/// Central place for player sound effects. Drag your clips into the fields below;
/// movement, attack and slide-damage scripts call the public Play methods on this.
/// Uses two AudioSources: one for one-shot clips (jump, dash, hits) so they can overlap
/// each other, and one dedicated to the slide loop so it can fade in/out instead of
/// hard cutting when you start or stop sliding.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip jumpClip;
    public AudioClip wallJumpClip;      // falls back to jumpClip if empty
    public AudioClip dashClip;
    public AudioClip slideStartClip;    // optional short whoosh the instant a slide begins
    public AudioClip slideLoopClip;     // looping grind/scrape while sliding
    public AudioClip slideHitClip;      // played when a slide connects with an enemy
    public AudioClip groundPoundLandClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;

    [Header("Slide Loop")]
    [Range(0f, 1f)] public float slideLoopVolume = 0.6f;
    public float slideLoopFadeTime = 0.08f;

    [Header("Mixing")]
    [Range(0f, 1f)] public float oneShotVolume = 0.85f;
    [Tooltip("Small random pitch range so repeated sounds (jump, dash) don't sound identical every time.")]
    public float pitchJitter = 0.05f;

    private AudioSource oneShotSource;
    private AudioSource loopSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        oneShotSource = GetComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.volume = 0f;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;
        oneShotSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        oneShotSource.PlayOneShot(clip, oneShotVolume);
    }

    public void PlayJump()      => PlayOneShot(jumpClip);
    public void PlayWallJump()  => PlayOneShot(wallJumpClip ? wallJumpClip : jumpClip);
    public void PlayDash()      => PlayOneShot(dashClip);
    public void PlaySlideHit()  => PlayOneShot(slideHitClip);
    public void PlayGroundPoundLand() => PlayOneShot(groundPoundLandClip);
    public void PlayHurt()      => PlayOneShot(hurtClip);
    public void PlayDeath()     => PlayOneShot(deathClip);

    /// <summary>Call once when a slide begins.</summary>
    public void StartSlideLoop()
    {
        PlayOneShot(slideStartClip);
        if (!slideLoopClip) return;

        if (loopSource.clip != slideLoopClip) loopSource.clip = slideLoopClip;
        if (!loopSource.isPlaying) loopSource.Play();
        FadeLoopTo(slideLoopVolume);
    }

    /// <summary>Call once when a slide ends (including cut short by jump/dash/pogo).</summary>
    public void StopSlideLoop() => FadeLoopTo(0f, stopOnDone: true);

    private void FadeLoopTo(float target, bool stopOnDone = false)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target, stopOnDone));
    }

    private IEnumerator FadeRoutine(float target, bool stopOnDone)
    {
        float start = loopSource.volume;
        float t = 0f;
        while (t < slideLoopFadeTime)
        {
            t += Time.deltaTime;
            loopSource.volume = Mathf.Lerp(start, target, t / slideLoopFadeTime);
            yield return null;
        }
        loopSource.volume = target;
        if (stopOnDone && target <= 0.0001f) loopSource.Stop();
    }
}