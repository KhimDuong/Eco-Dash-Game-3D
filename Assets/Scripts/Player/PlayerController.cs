using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 4-direction movement for Greenie (the cleanup robot) on the XZ ground
/// plane. Same feel as the 2D original — WASD, no jumping, no camera control — but
/// input Y maps to world Z and a CharacterController replaces the Rigidbody2D.
/// Other systems modify speed through EnterMud/ExitMud (Toxic Mud hazard) and
/// SetSpeedBoost (Green Sprout Energy Drink item).
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

    CharacterController body;
    Vector3 moveInput;            // on XZ, magnitude <= 1
    int mudContacts;              // how many overlapping mud zones we're inside
    float speedBoost = 1f;        // 1 = normal; >1 from energy drink
    float knockbackUntil;         // input-driven movement is suspended until this time
    Vector3 knockbackVelocity;    // carried while knocked back
    Coroutine speedBoostRoutine;

    void Awake()
    {
        body = GetComponent<CharacterController>();
        moveSpeed *= PlayerProgress.SpeedMultiplier; // permanent move-speed upgrade (M4 shop)
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
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

        body.Move((horizontal + Vector3.down * groundStickSpeed) * Time.deltaTime);
    }

    void ReadInput()
    {
        // Freeze steering while a dialogue line is up (M7 narrative layer).
        if (DialogueRunner.IsActive) { moveInput = Vector3.zero; return; }

        var kb = Keyboard.current;
        if (kb == null) { moveInput = Vector3.zero; return; }

        Vector3 dir = Vector3.zero;
        if (kb.wKey.isPressed) dir.z += 1f;   // 2D input Y -> world Z
        if (kb.sKey.isPressed) dir.z -= 1f;
        if (kb.dKey.isPressed) dir.x += 1f;
        if (kb.aKey.isPressed) dir.x -= 1f;

        moveInput = dir.sqrMagnitude > 1f ? dir.normalized : dir; // no diagonal speed boost
        if (moveInput != Vector3.zero) FacingDirection = moveInput;
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
    /// A zero impulse just roots Greenie in place (ManholeTrap). Any vertical component
    /// is dropped — knockback never lifts Greenie off the ground.
    /// </summary>
    public void ApplyKnockback(Vector3 velocity, float duration)
    {
        knockbackVelocity = new Vector3(velocity.x, 0f, velocity.z);
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
