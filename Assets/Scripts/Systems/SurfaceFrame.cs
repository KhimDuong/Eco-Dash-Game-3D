using UnityEngine;

/// <summary>
/// B9: which way is <b>up</b> for Greenie this frame, and the rotation that takes the world's
/// axes onto the surface he is standing on. One static owner, for the same reason
/// <see cref="PerspectiveMode"/> is one: the ground stick, the movement frame, the aim, the
/// visual's bob and turn, the first-person eye offset and the knockback direction all have to
/// agree on it <i>within the same frame</i>, and every one of them used to say
/// <c>Vector3.up</c> in its own hand.
///
/// <para><b>It is the identity while he is on the ground, and that is the whole safety
/// argument.</b> <see cref="Up"/> is <c>Vector3.up</c>, <see cref="Rotation"/> is
/// <c>Quaternion.identity</c>, and every expression that now routes through here collapses back
/// to exactly the arithmetic it replaced — <c>-Up * stick</c> is <c>Vector3.down * stick</c>,
/// <c>ProjectOnPlane(v, Up)</c> is <c>v.y = 0</c>. Level 2, the hub and the story scenes contain
/// no <see cref="Climbable"/> surface at all, so nothing there can ever leave the identity.</para>
///
/// <para><b>Physics reads the exact frame, presentation reads the blended one.</b> Attaching to
/// a wall swings "up" through 90° in one frame. The visual and the first-person camera must not
/// snap through that — hence <see cref="VisualRotation"/>, which turns at
/// <see cref="TurnSpeed"/> — but the ground stick must, or for the quarter-second of the blend
/// it points diagonally and shoves him a metre off the top of the mesa on every dismount. So
/// <see cref="Up"/> and <see cref="Rotation"/> snap and <see cref="VisualRotation"/> eases, and
/// the two are deliberately not the same value.</para>
///
/// <para><b>The frame's forward is up the wall.</b> <see cref="Rotation"/> maps local up onto the
/// surface normal and local forward onto the steepest uphill direction, so <c>W</c> climbs and
/// <c>A</c>/<c>D</c> traverse without <see cref="PlayerController"/> branching on anything —
/// exactly the trick B6 used to make WASD camera-relative. On the ground the steepest-uphill
/// direction is degenerate, which is the other reason the grounded case is hard-coded to the
/// identity rather than computed.</para>
///
/// <para>Statics survive Play (CLAUDE.md rule 4): a session that quit while on a wall would
/// otherwise start the next one with the world rotated 90°. Hence <see cref="ResetStatics"/>.</para>
/// </summary>
public static class SurfaceFrame
{
    /// <summary>Degrees per second the presentation frame rolls toward a newly attached surface.</summary>
    public const float TurnSpeed = 420f;

    /// <summary>True while Greenie is walking a surface that is not the ground plane.</summary>
    public static bool IsClimbing { get; private set; }

    /// <summary>
    /// Greenie's up: the surface normal while climbing, <c>Vector3.up</c> otherwise. Snaps.
    /// This is the physics answer — the ground stick and every knockback projection use it.
    /// </summary>
    public static Vector3 Up { get; private set; } = Vector3.up;

    /// <summary>
    /// Rotation from world axes onto the surface: local up → <see cref="Up"/>, local forward →
    /// straight up the wall. Identity on the ground. Snaps, and is what
    /// <see cref="PerspectiveMode.MoveFrame"/> composes with.
    /// </summary>
    public static Quaternion Rotation { get; private set; } = Quaternion.identity;

    /// <summary>
    /// The same rotation, eased toward <see cref="Rotation"/> at <see cref="TurnSpeed"/>. Used
    /// by the visual and the first-person camera so an attach reads as Greenie rolling onto the
    /// wall rather than as a cut. Reaches the identity <i>exactly</i> once a dismount settles,
    /// so a scene with nothing climbable in it never leaves it.
    /// </summary>
    public static Quaternion VisualRotation { get; private set; } = Quaternion.identity;

    /// <summary>The presentation frame's up. What the mesh, the bob and the eye offset use.</summary>
    public static Vector3 VisualUp => VisualRotation * Vector3.up;

    /// <summary>
    /// Attach to a surface with this normal. Called only by <see cref="WallClimber"/> — gameplay
    /// reads the frame and never authors it, the same split <see cref="GroundHeight.Profile"/>
    /// uses.
    /// </summary>
    internal static void Attach(Vector3 normal)
    {
        // Uphill is world up with the surface's own tilt taken out of it. On a dead-vertical
        // face that is exactly Vector3.up; the projection is here so a steep-but-not-vertical
        // face still gets a sane forward instead of a near-zero vector.
        Vector3 uphill = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (uphill.sqrMagnitude < 1e-6f) return;   // a ceiling or a floor: nothing to climb

        IsClimbing = true;
        Up = normal;
        Rotation = Quaternion.LookRotation(uphill.normalized, normal);
    }

    /// <summary>Return to the ground plane. Idempotent.</summary>
    internal static void Detach()
    {
        IsClimbing = false;
        Up = Vector3.up;
        Rotation = Quaternion.identity;
    }

    /// <summary>Advance the eased presentation frame. Driven by <see cref="WallClimber"/>.</summary>
    internal static void Advance(float deltaTime)
    {
        // Assign rather than return once they compare equal. Quaternion's == is a dot-product
        // comparison with a tolerance, so RotateTowards stops roughly 0.1 degrees short and the
        // eased frame keeps that residue for the rest of the session — statics survive Play, so
        // it is still there in the editor afterwards. One assignment makes "settles back to the
        // identity" true to the bit instead of true to a tolerance.
        if (VisualRotation == Rotation) { VisualRotation = Rotation; return; }
        VisualRotation = Quaternion.RotateTowards(VisualRotation, Rotation, TurnSpeed * deltaTime);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsClimbing = false;
        Up = Vector3.up;
        Rotation = Quaternion.identity;
        VisualRotation = Quaternion.identity;
    }
}
