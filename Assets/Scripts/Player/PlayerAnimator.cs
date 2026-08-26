using UnityEngine;

/// <summary>
/// Procedural "alive" motion for Greenie (the hover-bot) without an animation clip:
/// a gentle floating bob that quickens while moving, a subtle squash-and-stretch,
/// and a turn toward the move direction. Drives a child "Visual" transform so the
/// CharacterController capsule stays perfectly still.
///
/// 3D port note: the bob and squash are the same trick on the same axis (Y is up in
/// both projects); the 2D horizontal sprite flip becomes a yaw rotation of the visual
/// child, so Greenie faces all four directions instead of just left/right.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] PlayerController controller;
    [Tooltip("Child transform holding the mesh. Bobs/squashes/turns independently of the body.")]
    [SerializeField] Transform visual;

    [Header("Hover bob")]
    [SerializeField] float idleBobAmplitude = 0.05f;
    [SerializeField] float moveBobAmplitude = 0.12f;
    [SerializeField] float idleBobSpeed = 3f;
    [SerializeField] float moveBobSpeed = 10f;

    [Header("Squash & turn")]
    [Tooltip("How much the mesh stretches/squashes through the bob while moving.")]
    [SerializeField] float moveSquash = 0.08f;
    [Tooltip("Degrees per second the visual turns to face the move direction.")]
    [SerializeField] float turnSpeed = 720f;

    /// <summary>
    /// A vertical offset added to the visual's rest height, in metres — negative sinks Greenie.
    /// Used by <see cref="WaterWade"/> so he reads as standing <i>in</i> the spring.
    ///
    /// <para>This class owns <c>visual.localPosition</c>: it rewrites it from
    /// <c>baseLocalPos</c> every frame, so anything that moves the mesh by writing the transform
    /// directly is scrubbed off on the very next Update — the same ownership trap as
    /// <see cref="HitFlash"/> and an enemy's resting colour. Offsets come through here.</para>
    /// </summary>
    public float SinkOffset { get; set; }

    Vector3 baseLocalPos;
    Vector3 baseScale;
    float bobPhase;
    float amp;       // smoothed bob amplitude
    float bobHz;     // smoothed bob speed

    void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (visual != null)
        {
            baseLocalPos = visual.localPosition;
            baseScale = visual.localScale;
        }
        amp = idleBobAmplitude;
        bobHz = idleBobSpeed;
    }

    void Update()
    {
        if (visual == null) return;

        bool moving = controller != null && controller.IsMoving;
        Vector3 facing = controller != null ? controller.FacingDirection : Vector3.back;

        // Ease amplitude/speed so starting and stopping don't pop.
        amp = Mathf.Lerp(amp, moving ? moveBobAmplitude : idleBobAmplitude, Time.deltaTime * 8f);
        bobHz = Mathf.Lerp(bobHz, moving ? moveBobSpeed : idleBobSpeed, Time.deltaTime * 8f);

        bobPhase += Time.deltaTime * bobHz * Mathf.PI * 2f;
        float wave = Mathf.Sin(bobPhase);

        visual.localPosition = baseLocalPos + new Vector3(0f, wave * amp + SinkOffset, 0f);

        // Squash/stretch only matters while moving; volume roughly preserved.
        float s = moving ? moveSquash : 0f;
        float flatten = 1f - wave * s * 0.5f;
        visual.localScale = new Vector3(
            baseScale.x * flatten,
            baseScale.y * (1f + wave * s),
            baseScale.z * flatten);

        // Turn toward the facing direction; keeps the last heading when standing still.
        if (facing.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
