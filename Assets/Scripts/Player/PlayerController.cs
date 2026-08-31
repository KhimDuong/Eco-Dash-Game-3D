using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movement for Greenie (the cleanup robot) on the XZ ground plane. Same feel as the
/// 2D original — WASD, no jumping — but input Y maps to world Z and a
/// CharacterController replaces the Rigidbody2D. Other systems modify speed through
/// EnterMud/ExitMud (Toxic Mud hazard) and SetSpeedBoost (Green Sprout Energy Drink item).
///
/// <para><b>B6 made the input camera-relative.</b> WASD is read in screen axes and then
/// turned into world axes by <see cref="PerspectiveMode.MoveFrame"/>. That rotation is the
/// identity while the ¾ camera is live — its yaw is locked at 0, so W is still world +Z and
/// every top-down path behaves exactly as it did before — and becomes the look yaw in first
/// person, where W has to mean "forward" or the controls invert the moment the player turns
/// round. There is no branch on the view mode here; there is a multiply.</para>
///
/// <para><b>B9 added a second frame the same way.</b> <see cref="SurfaceFrame"/> says which way
/// is up; <c>MoveFrame</c> now carries it, so on a wall <c>W</c> climbs and <c>A</c>/<c>D</c>
/// traverse with no branch here either. The ground stick went from <c>Vector3.down</c> to
/// <c>-SurfaceFrame.Up</c> — the same vector on the ground, the direction into the rock on a
/// wall — and that one substitution is the whole of the climbing change to this file. The
/// <see cref="CharacterController"/> is unchanged and still world-Y aligned;
/// <see cref="WallClimber"/> explains what that costs.</para>
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [Tooltip("Speed multiplier while standing in Toxic Mud (bùn lầy hóa chất).")]
    [SerializeField] float mudSlowMultiplier = 0.5f;

    [Header("Ground")]
    [Tooltip("Constant downward speed that keeps the capsule planted on slopes and steps. " +
             "Gravity is never a gameplay mechanic in Eco-Dash — nothing jumps or falls.")]
    [SerializeField] float groundStickSpeed = 9.81f;

    [Header("Animation (optional)")]
    [SerializeField] Animator animator;

    /// <summary>
    /// Last non-zero move direction on the ground plane (y is always 0); used as the
    /// aim for shooting and the facing for the visual child. Starts pointing at the
    /// camera (-Z), which is the 3D equivalent of the 2D original's Vector2.down.
    /// </summary>
    public Vector3 FacingDirection { get; private set; } = Vector3.back;

    /// <summary>True while a movement key is held (drives walk vs idle visuals).</summary>
    public bool IsMoving => moveInput != Vector3.zero;

    /// <summary>
    /// Is the wall climber present <i>and</i> switched on? Both halves matter: a disabled
    /// MonoBehaviour still answers a direct method call, so a plain null check kept Greenie
    /// climbing after <c>WallClimber.enabled = false</c> — which made the B9 control run,
    /// the one that is supposed to show the pre-B9 behaviour, climb the mesa.
    /// </summary>
    bool Climbing => climber != null && climber.isActiveAndEnabled;

    CharacterController body;
    WallClimber climber;          // optional (B9); null everywhere it is not on the prefab
    Vector3 moveInput;            // in the surface frame, magnitude <= 1
    int mudContacts;              // how many overlapping mud zones we're inside
    float speedBoost = 1f;        // 1 = normal; >1 from energy drink
    float knockbackUntil;         // input-driven movement is suspended until this time
    Vector3 knockbackVelocity;    // carried while knocked back
    Coroutine speedBoostRoutine;

    void Awake()
    {
        body = GetComponent<CharacterController>();
        climber = GetComponent<WallClimber>();
        moveSpeed *= PlayerProgress.SpeedMultiplier; // permanent move-speed upgrade (M4 shop)
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // The climber runs first and on last frame's input: the input it judges was read in the
        // frame it was read in, and ReadInput below then builds this frame's input in whichever
        // frame the climber just settled on.
        if (Climbing) climber.Tick(moveInput, Time.deltaTime);
        ReadInput();
        Move();
        UpdateAnimator();
    }

    void Move()
    {
        Vector3 horizontal;
        if (Time.time < knockbackUntil)
        {
            // While knocked back, let the impulse carry the body — don't steer.
            horizontal = knockbackVelocity;
        }
        else
        {
            knockbackVelocity = Vector3.zero;
            float speed = moveSpeed * speedBoost * (mudContacts > 0 ? mudSlowMultiplier : 1f);
            horizontal = moveInput * speed;
        }

        // -SurfaceFrame.Up is Vector3.down on the ground, to the bit; on a wall it points into
        // the rock, which is what holds Greenie against it.
        Vector3 velocity = horizontal - SurfaceFrame.Up * groundStickSpeed;
        if (Climbing) velocity = climber.Constrain(velocity, Time.deltaTime);
        body.Move(velocity * Time.deltaTime);
    }

    void ReadInput()
    {
        // Freeze steering while a dialogue line is up (M7 narrative layer).
        if (DialogueRunner.IsActive) { moveInput = Vector3.zero; return; }

        var kb = Keyboard.current;
        if (kb == null) { moveInput = Vector3.zero; return; }

        Vector3 dir = Vector3.zero;
        if (kb.wKey.isPressed) dir.z += 1f;   // 2D input Y -> world Z (screen "up")
        if (kb.sKey.isPressed) dir.z -= 1f;
        if (kb.dKey.isPressed) dir.x += 1f;
        if (kb.aKey.isPressed) dir.x -= 1f;

        if (dir.sqrMagnitude > 1f) dir.Normalize();       // no diagonal speed boost
        moveInput = PerspectiveMode.MoveFrame * dir;      // identity under the ¾ camera

        // Facing is the aim as well as the animator's heading. In first person it follows the
        // look, so Greenie shoots where the player is looking even while strafing or standing
        // still; under the ¾ camera it keeps following the move direction, as it always has.
        if (PerspectiveMode.IsFirstPerson) FacingDirection = PerspectiveMode.AimForward;
        else if (moveInput != Vector3.zero) FacingDirection = moveInput;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", FacingDirection.x);
        animator.SetFloat("MoveY", FacingDirection.z); // the 2D "MoveY" axis is world Z here
        animator.SetBool("IsMoving", IsMoving);
    }

    // --- Hooks for hazards / items -----------------------------------------

    public void EnterMud() => mudContacts++;
    public void ExitMud() => mudContacts = Mathf.Max(0, mudContacts - 1);

    /// <summary>
    /// Shove the body and suspend input-steering for <paramref name="duration"/> seconds.
    /// A zero impulse just roots Greenie in place (ManholeTrap). The component along
    /// <see cref="SurfaceFrame.Up"/> is dropped — knockback never lifts Greenie off the surface
    /// he is on, and on the ground that projection is exactly the old <c>velocity.y = 0</c>.
    /// </summary>
    public void ApplyKnockback(Vector3 velocity, float duration)
    {
        knockbackVelocity = Vector3.ProjectOnPlane(velocity, SurfaceFrame.Up);
        knockbackUntil = Time.time + duration;
    }

    /// <summary>Set the temporary speed boost multiplier (1 = no boost).</summary>
    public void SetSpeedBoost(float multiplier) => speedBoost = Mathf.Max(0.1f, multiplier);

    /// <summary>
    /// Apply a timed move-speed multiplier (Green Sprout Energy Drink). Picking up
    /// another drink restarts the timer rather than stacking the multiplier.
    /// </summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        speedBoost = Mathf.Max(0.1f, multiplier);
        yield return new WaitForSeconds(duration);
        speedBoost = 1f;
        speedBoostRoutine = null;
    }
}
