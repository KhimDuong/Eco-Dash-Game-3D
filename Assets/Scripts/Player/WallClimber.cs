using UnityEngine;

/// <summary>
/// B9: the ant. Attaches Greenie to a <see cref="Climbable"/> vertical face when he walks into
/// one and keeps pushing, holds him on it while he traverses, and puts him back on the ground at
/// the top or the bottom. It is the only writer of <see cref="SurfaceFrame"/>; everything else in
/// the game reads that frame and never learns that climbing exists.
///
/// <para><b>The <see cref="CharacterController"/> is kept, and the backlog said it could not be.</b>
/// The claim was that ant-walking needs a <c>Rigidbody</c> plus a surface-aligned controller
/// because "a <c>CharacterController</c>'s capsule is permanently world-Y aligned and cannot be
/// re-oriented". The capsule really cannot — but <see cref="CharacterController.Move"/> takes a
/// <i>world-space delta</i> and has no opinion about gravity, so a capsule pressed against a wall
/// climbs it the moment you hand it a delta pointing up the face. What the fixed capsule costs is
/// stated plainly below; it is a hitbox caveat, not a blocker, and paying it keeps the one script
/// the game's feel rests on unrewritten. Same shape of finding as B8, one layer up.</para>
///
/// <para><b>The capsule does not rotate, so the hitbox on a wall is not the silhouette.</b>
/// Greenie's mesh lies flat against the face (soles on the rock, head pointing out of it) while
/// his collider stays a 1.15 m upright capsule flush with that face — roughly the same volume
/// turned 90 degrees, hugging the wall instead of standing out of it. Nothing in the game can
/// exploit the difference: the enemies are NavMesh-bound to the ground and cannot reach a wall,
/// seeds fly at ground clearance (B8), and firing is disabled up here on purpose — see
/// <see cref="PlayerShooter"/>. It would matter the moment something shot at him on a wall, and
/// that is the line where this shortcut stops being free.</para>
///
/// <para><b>Climbing is opt-in per surface</b> (<see cref="Climbable"/>) because every boundary
/// wall in the project is also a vertical face. And it needs no new key: the control contract in
/// CLAUDE.md has none free, so pushing into the rock for <see cref="attachDwell"/> seconds is the
/// gesture. The dwell is what stops a brush past the mesa turning into a climb.</para>
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class WallClimber : MonoBehaviour
{
    [Header("Attaching")]
    [Tooltip("Surfaces to probe. Left at 0 this resolves to the Obstacle layer, where " +
             "TerrainKit's per-column mesa colliders live.")]
    [SerializeField] LayerMask climbMask = 0;
    [Tooltip("Seconds of pushing into a climbable face before Greenie commits to it. Without a " +
             "dwell, walking past the mesa at a glancing angle starts a climb.")]
    [SerializeField] float attachDwell = 0.25f;
    [Tooltip("How far past the capsule's own radius the attach probe reaches, in metres.")]
    [SerializeField] float attachReach = 0.35f;
    [Tooltip("A face counts as a wall past this angle from world up. Well clear of the " +
             "CharacterController's 45 degree slopeLimit so a walkable slope is never stolen " +
             "from it — and clear of B8's 24.7 degree ground, which must never attach.")]
    [SerializeField] float minWallAngle = 60f;
    [Tooltip("Seconds after a dismount before Greenie may attach again. Long enough for the " +
             "top-out nudge to finish before the next probe can re-grab the face he just left.")]
    [SerializeField] float reattachDelay = 0.35f;

    [Header("Staying on")]
    [Tooltip("How far the foot probe reaches into the face, past the capsule radius. This is " +
             "what finds the next column across a seam.")]
    [SerializeField] float holdReach = 0.4f;
    [Tooltip("Height above the feet the foot probe is cast from. Small on purpose: it is what " +
             "decides Greenie has cleared the lip, and it must not clear it before his capsule " +
             "does or the dismount nudge shoves him into the rock.")]
    [SerializeField] float footHeight = 0.08f;
    [Tooltip("Fraction of Greenie's ground speed he moves at on a wall. At the full 5 m/s a " +
             "1.4 m tier of the mesa is over in 0.28 s, which reads as teleporting up the rock " +
             "rather than climbing it — and leaves the surface frame on screen for four frames.")]
    [SerializeField] float climbSpeedFactor = 0.5f;

    /// <summary>
    /// How far above the ground he must have climbed before returning to it counts as a
    /// dismount rather than as never having left.
    /// </summary>
    const float MinRise = 0.25f;

    /// <summary>The collider Greenie is on, or null while he is on the ground.</summary>
    public Collider Surface { get; private set; }

    CharacterController body;
    Vector3 normal = Vector3.up;
    float pushTime;
    float attachableAt;
    float highWater;              // the highest he has been on this surface, in world Y

    void Awake()
    {
        body = GetComponent<CharacterController>();
        if (climbMask == 0) climbMask = LayerMask.GetMask("Obstacle");
    }

    void OnDisable()
    {
        if (SurfaceFrame.IsClimbing) SurfaceFrame.Detach();
        Surface = null;
    }

    /// <summary>
    /// Decide whether Greenie is on a wall this frame, and ease the presentation frame toward
    /// the answer. Called by <see cref="PlayerController"/> at the top of its Update with the
    /// previous frame's move input — the frame that input was read in is the frame it belongs to.
    /// </summary>
    public void Tick(Vector3 moveInput, float deltaTime)
    {
        if (SurfaceFrame.IsClimbing) Hold();
        else TryAttach(moveInput, deltaTime);
        SurfaceFrame.Advance(deltaTime);
    }

    /// <summary>
    /// Filter a frame's velocity so it cannot leave the side of the surface. Returns it
    /// untouched while Greenie is on the ground, which is every frame in every scene that has
    /// nothing climbable in it.
    ///
    /// <para>Off the <i>top</i> is not an error — it is the dismount, and it belongs here rather
    /// than in <see cref="Hold"/> because only the move that is about to happen knows whether he
    /// is going up or sideways. Off the <i>side</i> is an edge, and is cancelled: a climber who
    /// slides off a column at 4 m and is then handed to the ground stick falls the whole way at
    /// 9.81 m/s, which is the "launched or stuck" failure this item forbids.</para>
    /// </summary>
    public Vector3 Constrain(Vector3 velocity, float deltaTime)
    {
        if (!SurfaceFrame.IsClimbing || deltaTime <= 0f) return velocity;

        Vector3 uphill = SurfaceFrame.Rotation * Vector3.forward;
        Vector3 across = SurfaceFrame.Rotation * Vector3.right;
        float rise = Vector3.Dot(velocity, uphill);
        Vector3 side = across * Vector3.Dot(velocity, across);

        // Rising past the lip: step over onto the top face and hand him back to the ground.
        if (rise > 0f && !Anchored(transform.position + uphill * rise * deltaTime))
        {
            Dismount(overTheTop: true);
            return Vector3.zero;
        }

        // Traversing off the end of the rock: cancel that component, keep the rest.
        if (side.sqrMagnitude > 1e-6f && !Anchored(transform.position + side * deltaTime))
            velocity -= side;

        // Slow the movement across the face, but not the press into it: the stick is what holds
        // him on, and scaling it too would let him drift off the rock at exactly the same rate.
        Vector3 tangent = Vector3.ProjectOnPlane(velocity, SurfaceFrame.Up);
        return velocity - tangent + tangent * climbSpeedFactor;
    }

    // --- Attaching and holding ------------------------------------------------

    void TryAttach(Vector3 moveInput, float deltaTime)
    {
        Vector3 push = new Vector3(moveInput.x, 0f, moveInput.z);
        if (Time.time < attachableAt || push.sqrMagnitude < 0.01f) { pushTime = 0f; return; }
        push.Normalize();

        // Cast from mid-body rather than from the feet: the lowest tier of the mesa meets B8's
        // flat zone at its own base, and a probe down there can find the ground before the face.
        Vector3 from = transform.position + Vector3.up * (body.height * 0.5f);
        if (!Physics.Raycast(from, push, out var hit, body.radius + attachReach, climbMask,
                             QueryTriggerInteraction.Ignore)
            || !hit.collider.TryGetComponent<Climbable>(out _)
            || Vector3.Angle(hit.normal, Vector3.up) < minWallAngle)
        {
            pushTime = 0f;
            return;
        }

        pushTime += deltaTime;
        if (pushTime < attachDwell) return;

        pushTime = 0f;
        normal = hit.normal;
        Surface = hit.collider;
        highWater = transform.position.y;
        SurfaceFrame.Attach(normal);
    }

    void Hold()
    {
        // Re-probe every frame: the mesa is 18 separate column colliders, so traversing a face
        // hands Greenie from one to the next, and the normal has to come from whichever one is
        // actually under his feet now.
        if (Foot(transform.position, out var hit) && hit.collider.TryGetComponent<Climbable>(out _))
        {
            normal = hit.normal;
            Surface = hit.collider;
            SurfaceFrame.Attach(normal);
        }
        else
        {
            // The rock is gone and no move of his asked for it — knockback, or a column that
            // stopped existing. Stand him up where he is rather than leaving him in mid-air.
            Dismount(overTheTop: false);
            return;
        }

        // Back at ground level: stop climbing, so the last step off the rock is an ordinary one.
        //
        // The height he has actually reached is what makes this a dismount rather than a veto.
        // Every climb *starts* at ground level, so testing the current height alone cancels the
        // attach on the frame after it happens — which is exactly what it did: 15 attach/dismount
        // cycles in nine seconds of holding W, and Greenie never left the floor. You cannot bottom
        // out of a climb you have not begun.
        float pos = transform.position.y;
        highWater = Mathf.Max(highWater, pos);
        float ground = GroundHeight.At(transform.position);
        if (highWater - ground > MinRise && pos - ground <= 0.08f)
            Dismount(overTheTop: false);
    }

    void Dismount(bool overTheTop)
    {
        // Step in over the lip before letting go. The foot probe only misses once his whole
        // capsule is above the top face, so this move is unobstructed; without it the ground
        // stick catches the lip and drops him straight back down the way he came.
        if (overTheTop) body.Move(-normal * (body.radius + 0.3f));

        Surface = null;
        attachableAt = Time.time + reattachDelay;
        SurfaceFrame.Detach();
    }

    // --- Probes ---------------------------------------------------------------

    bool Anchored(Vector3 at) => Foot(at, out _);

    /// <summary>Is there rock in front of Greenie's feet at <paramref name="at"/>?</summary>
    bool Foot(Vector3 at, out RaycastHit hit) =>
        Physics.Raycast(at + Vector3.up * footHeight, -normal, out hit, body.radius + holdReach,
                        climbMask, QueryTriggerInteraction.Ignore);
}
