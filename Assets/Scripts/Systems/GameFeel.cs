using System.Collections;
using UnityEngine;

/// <summary>
/// C4's punch, in one place: the camera shake every ported caller already knew about, and
/// the hit-stop that makes a kill land. Both are static entry points so callers stay
/// one-liners — <c>GameFeel.Shake(...)</c> instead of the
/// <c>if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(...)</c> that was
/// getting copied around — and the tuning constants live here rather than being scattered
/// as serialized fields across four enemy prefabs nobody wants to rebuild.
///
/// <para><b>Hit-stop slows the clock, it does not stop it.</b> The obvious implementation
/// is <c>Time.timeScale = 0</c> for a few frames, and in this project that is a trap: the
/// clock is a shared resource with five other owners (<see cref="PauseController"/>,
/// <see cref="TutorialPopup"/>, <c>DialogueRunner</c>, <see cref="ShopController"/>, the
/// end screens), and every one of them parks it at exactly 0. A hit-stop that also parks it
/// at 0 cannot tell, when its wait ends, whether the 0 it sees is still its own or a
/// dialogue that opened in the meantime — and restoring the wrong one un-pauses a modal
/// under the player. Crawling at <see cref="StopScale"/> instead makes ownership
/// checkable: the restore only fires if the clock is still exactly where hit-stop left it.
/// At 2% speed a 0.09 s stop reads as a freeze anyway, and coroutines that measure scaled
/// time (the Mega-Smog's collapse) keep inching forward instead of deadlocking.</para>
/// </summary>
public class GameFeel : MonoBehaviour
{
    /// <summary>How slow the world runs during a hit-stop. Not 0 — see the class remarks.</summary>
    public const float StopScale = 0.02f;

    /// <summary>Killing an ordinary enemy. Short enough to feel like weight, not lag.</summary>
    public const float StopSmall = 0.045f;
    /// <summary>Taking a hit yourself.</summary>
    public const float StopHurt = 0.06f;
    /// <summary>A boss dying, or the Slime King splitting — the beats worth interrupting for.</summary>
    public const float StopBig = 0.12f;

    static GameFeel runner;
    static bool stopping;

    // Fast Enter Play Mode keeps statics alive between sessions: quitting play mode in the
    // middle of a stop would otherwise leave `stopping` latched true, and every hit-stop for
    // the rest of the editor session would silently decline.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { runner = null; stopping = false; }

    /// <summary>
    /// Shake the camera. Magnitude keeps its 2D meaning (peak offset in metres) — see
    /// <see cref="CameraFollow.Shake"/>. A no-op in a scene with no rig, so probes and the
    /// UI-only scenes can call it freely.
    /// </summary>
    public static void Shake(float duration, float magnitude)
    {
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(duration, magnitude);
        if (PerspectiveRig.Instance != null) PerspectiveRig.Instance.Shake(duration, magnitude);
    }

    /// <summary>
    /// Crawl the world for <paramref name="seconds"/> of real time. Declines while anything
    /// else owns the clock — a hit landing during a dialogue must not shove the game back
    /// into motion behind the text box.
    /// </summary>
    public static void HitStop(float seconds)
    {
        if (seconds <= 0f || stopping) return;
        if (!Application.isPlaying) return;
        if (!Mathf.Approximately(Time.timeScale, 1f)) return;   // paused, in dialogue, or shopping

        EnsureRunner();
        if (runner != null) runner.StartCoroutine(StopRoutine(seconds));
    }

    static IEnumerator StopRoutine(float seconds)
    {
        stopping = true;
        Time.timeScale = StopScale;
        yield return new WaitForSecondsRealtime(seconds);

        // Only take the clock back if it is still the one we set. If a modal opened during
        // those few milliseconds it now reads 0, and it is that modal's to restore.
        if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = 1f;
        stopping = false;
    }

    // The runner outlives scene loads on purpose: a stop that starts as a boss dies must
    // still finish if the death completes the level, and a coroutine on a scene object
    // would die with the scene and leave the world crawling at 2%.
    static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("~GameFeel");
        runner = go.AddComponent<GameFeel>();
        DontDestroyOnLoad(go);
    }

    // Belt and braces for teardown (play-mode exit, quit): the coroutine itself handles the
    // ordinary cases, including a scene load mid-stop.
    void OnDisable()
    {
        if (!stopping) return;
        if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = 1f;
        stopping = false;
    }
}
