using UnityEngine;

/// <summary>
/// B6: which of the game's two framings is live, and — while first person is — where the
/// player is looking. One static owner, because three unrelated systems have to agree on the
/// same yaw within the same frame: the camera rig points there, <see cref="PlayerController"/>
/// steers WASD there, and <see cref="PlayerShooter"/> fires there. A yaw held on the camera
/// and read through <c>Camera.main</c> would be a frame behind the movement that caused it,
/// and would wobble every time <see cref="GameFeel"/> shook the rig.
///
/// <para><b>The frame is the whole trick.</b> <see cref="MoveFrame"/> is the identity rotation
/// while the ¾ camera is live, so every top-down code path behaves exactly as it did before
/// B6 — <c>W</c> is still world +Z — and becomes the look yaw in first person, where <c>W</c>
/// has to mean "forward" or the controls invert the moment the player turns around. Nothing
/// branches on the view mode; it just multiplies by this.</para>
///
/// <para><b>B9 composed a second frame in front of this one.</b> <see cref="MoveFrame"/> and
/// <see cref="LookRotation"/> are now multiplied by <see cref="SurfaceFrame.Rotation"/>, which
/// is the identity everywhere except on a climbable wall. The two frames answer different
/// questions and stack cleanly: the surface frame says which way is up and which way is uphill,
/// this one says which way the player is looking within that. On a wall the composition makes
/// <c>W</c> climb and the mouse yaw swing around the wall normal, so first person on a wall has
/// the ant's answer to "which way is up" — <i>his</i> up — and the horizon rolls with him.</para>
///
/// <para>Statics survive Play (CLAUDE.md rule 4), which is exactly what the "toggle survives a
/// scene change" requirement wants — the mode outlives <c>LoadScene</c> on purpose — but it
/// must not outlive the play session, or the game starts in whatever view the last run ended
/// in. Hence <see cref="ResetStatics"/>.</para>
/// </summary>
public static class PerspectiveMode
{
    public enum View { TopDown, FirstPerson }

    /// <summary>How far up/down first person can look, in degrees.</summary>
    public const float MaxPitch = 80f;

    public static View Current { get; private set; } = View.TopDown;

    public static bool IsFirstPerson => Current == View.FirstPerson;

    /// <summary>Look yaw in degrees. Meaningful in first person; 0 under the ¾ camera.</summary>
    public static float LookYaw { get; private set; }

    /// <summary>Look pitch in degrees, Unity's sign convention (positive looks down).</summary>
    public static float LookPitch { get; private set; }

    /// <summary>The yaw WASD and aiming are expressed in. Always 0 under the ¾ camera.</summary>
    public static float MoveYaw => IsFirstPerson ? LookYaw : 0f;

    /// <summary>
    /// Rotation that takes screen-space WASD into world space. Identity in top-down on the
    /// ground — both factors are the identity there, so every pre-B6 path is untouched.
    /// </summary>
    public static Quaternion MoveFrame => SurfaceFrame.Rotation * Quaternion.Euler(0f, MoveYaw, 0f);

    /// <summary>
    /// Unit vector the player is aiming along. On the ground it lies on XZ and seeds still fly
    /// flat (rule 1); on a wall it lies in the wall plane, which is why firing is disabled there.
    /// </summary>
    public static Vector3 AimForward => MoveFrame * Vector3.forward;

    /// <summary>
    /// Full look rotation for the first-person camera, pitch included — and, on a wall, rolled
    /// into the surface frame so the rock reads as the floor.
    /// </summary>
    public static Quaternion LookRotation =>
        SurfaceFrame.VisualRotation * Quaternion.Euler(LookPitch, LookYaw, 0f);

    public static void Toggle() => Set(IsFirstPerson ? View.TopDown : View.FirstPerson);

    /// <summary>
    /// Switch framing. The look angles reset to the ¾ camera's own heading (yaw 0, level)
    /// rather than to Greenie's facing: at the instant of the toggle <c>W</c> then still moves
    /// the same way it did the frame before, so a player who presses P mid-fight keeps walking
    /// where they were walking instead of being spun round by the camera.
    /// </summary>
    public static void Set(View view)
    {
        LookYaw = 0f;
        LookPitch = 0f;
        Current = view;
    }

    /// <summary>Feed a frame of look input, in degrees. Ignored outside first person.</summary>
    public static void Look(float yawDelta, float pitchDelta)
    {
        if (!IsFirstPerson) return;
        LookYaw = Mathf.Repeat(LookYaw + yawDelta + 180f, 360f) - 180f;
        LookPitch = Mathf.Clamp(LookPitch - pitchDelta, -MaxPitch, MaxPitch);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = View.TopDown;
        LookYaw = 0f;
        LookPitch = 0f;
    }
}
